using History.ApiService.DataTypes;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using MongoDB.Driver;
using System.Security.Cryptography;

namespace History.ApiService.Services;

public class MigrationService(IMongoDatabase database, ILogger<MigrationService> logger) : IHostedService
{
    private readonly IMongoCollection<MigrationRecord> _migrations = database.GetCollection<MigrationRecord>("Migrations");
    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<Post> _publicPostCollection = database.GetCollection<Post>("PublicPosts");
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");
    private readonly IMongoCollection<InviteCode> _inviteCodeCollection = database.GetCollection<InviteCode>("InviteCodes");

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Ensure unique index on version
        var indexKeys = Builders<MigrationRecord>.IndexKeys.Ascending(x => x.Version);
        var indexModel = new CreateIndexModel<MigrationRecord>(indexKeys, new CreateIndexOptions { Unique = true });
        await _migrations.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);

        await RunMigrationsAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task RunMigrationsAsync()
    {
        await ApplyMigrationAsync(1, "MigrateHashtagsToHashtagContent", MigrateHashtagsToHashtagContentAsync);
        await ApplyMigrationAsync(2, "IssueInviteCodesToExistingUsers", IssueInviteCodesToExistingUsersAsync);
        await ApplyMigrationAsync(3, "IssueInviteCodesToUsersWithNoCodes", IssueInviteCodesToUsersWithNoCodesAsync);
        await ApplyMigrationAsync(4, "BackfillCommentSearchIndex", BackfillCommentSearchIndexAsync);
        await ApplyMigrationAsync(5, "NormalizeExternalUrlThumbnails", NormalizeExternalUrlThumbnailsAsync);
    }

    private async Task ApplyMigrationAsync(int version, string name, Func<Task> migration)
    {
        var filter = Builders<MigrationRecord>.Filter.Eq(x => x.Version, version);
        var existing = await _migrations.Find(filter).FirstOrDefaultAsync();

        if (existing is not null)
        {
            logger.LogInformation("[MIGRATION] v{Version} ({Name}) already applied, skipping.", version, name);
            return;
        }

        logger.LogInformation("[MIGRATION] Applying v{Version} ({Name})...", version, name);
        await migration();

        await _migrations.InsertOneAsync(new MigrationRecord
        {
            Version = version,
            Name = name,
            AppliedAt = DateTime.UtcNow
        });

        logger.LogInformation("[MIGRATION] v{Version} ({Name}) applied successfully.", version, name);
    }

    /// <summary>
    /// v1: Migrate legacy Post.Hashtags into HashtagContent entries appended to Post.Contents.
    /// Inserts a space TextContent between each HashtagContent.
    /// Clears Post.Hashtags after migration.
    /// </summary>
    private async Task MigrateHashtagsToHashtagContentAsync()
    {
        await MigrateCollectionHashtagsAsync(_postCollection, "Posts");
        await MigrateCollectionHashtagsAsync(_publicPostCollection, "PublicPosts");
    }

    private async Task MigrateCollectionHashtagsAsync(IMongoCollection<Post> collection, string collectionName)
    {
        // Find posts that have non-empty Hashtags and no HashtagContent in Contents
        var filter = Builders<Post>.Filter.And(
            Builders<Post>.Filter.SizeGt(x => x.Hashtags, 0)
        );

        var posts = await collection.Find(filter).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} posts with legacy hashtags in {Collection}.", posts.Count, collectionName);

        var migratedCount = 0;
        foreach (var post in posts)
        {
            // Skip if already has HashtagContent
            if (post.Contents.OfType<HashtagContent>().Any()) continue;

            var hashtags = post.Hashtags ?? [];
            if (hashtags.Count == 0) continue;

            for (int i = 0; i < hashtags.Count; i++)
            {
                if (i > 0) post.Contents.Add(new TextContent { Text = " " });
                post.Contents.Add(new HashtagContent { Tag = hashtags[i] });
            }

            post.Hashtags = [];

            var updateFilter = Builders<Post>.Filter.Eq(x => x.Id, post.Id);
            var update = Builders<Post>.Update
                .Set(x => x.Contents, post.Contents)
                .Set(x => x.Hashtags, post.Hashtags);

            await collection.UpdateOneAsync(updateFilter, update);
            migratedCount++;
        }

        logger.LogInformation("[MIGRATION] Migrated {Count} posts in {Collection}.", migratedCount, collectionName);
    }

    /// <summary>
    /// v2: Issue 7 invite codes to each existing user.
    /// </summary>
    private async Task IssueInviteCodesToExistingUsersAsync()
    {
        // Ambiguous characters (I, O, 0, 1) excluded
        const string codeCharset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        const int codeLength = 8;
        const int codesPerUser = 7;

        var users = await _userCollection.Find(FilterDefinition<User>.Empty).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} users to issue invite codes to.", users.Count);

        var totalIssued = 0;
        foreach (var user in users)
        {
            var codes = new List<InviteCode>();
            for (int i = 0; i < codesPerUser; i++)
            {
                string code;
                while (true)
                {
                    var bytes = RandomNumberGenerator.GetBytes(codeLength);
                    var chars = new char[codeLength];
                    for (int j = 0; j < codeLength; j++) chars[j] = codeCharset[bytes[j] % codeCharset.Length];
                    code = new string(chars);
                    var existing = await _inviteCodeCollection.Find(x => x.Code == code).FirstOrDefaultAsync();
                    if (existing == null) break;
                }

                codes.Add(new InviteCode
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Code = code,
                    OwnerId = user.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _inviteCodeCollection.InsertManyAsync(codes);
            totalIssued += codes.Count;
        }

        logger.LogInformation("[MIGRATION] Issued {Count} invite codes to {UserCount} users.", totalIssued, users.Count);
    }

    /// <summary>
    /// v3: Issue 7 invite codes to users who have no invite codes at all.
    /// </summary>
    private async Task IssueInviteCodesToUsersWithNoCodesAsync()
    {
        // Ambiguous characters (I, O, 0, 1) excluded
        const string codeCharset = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        const int codeLength = 8;
        const int codesPerUser = 7;

        var ownerIds = await _inviteCodeCollection.DistinctAsync(x => x.OwnerId, FilterDefinition<InviteCode>.Empty);
        var ownerIdSet = (await ownerIds.ToListAsync()).ToHashSet();

        var users = await _userCollection.Find(u => !ownerIdSet.Contains(u.Id)).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} users with no invite codes.", users.Count);

        var totalIssued = 0;
        foreach (var user in users)
        {
            var codes = new List<InviteCode>();
            for (int i = 0; i < codesPerUser; i++)
            {
                string code;
                while (true)
                {
                    var bytes = RandomNumberGenerator.GetBytes(codeLength);
                    var chars = new char[codeLength];
                    for (int j = 0; j < codeLength; j++) chars[j] = codeCharset[bytes[j] % codeCharset.Length];
                    code = new string(chars);
                    var existing = await _inviteCodeCollection.Find(x => x.Code == code).FirstOrDefaultAsync();
                    if (existing == null) break;
                }

                codes.Add(new InviteCode
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Code = code,
                    OwnerId = user.Id,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _inviteCodeCollection.InsertManyAsync(codes);
            totalIssued += codes.Count;
        }

        logger.LogInformation("[MIGRATION] Issued {Count} invite codes to {UserCount} users.", totalIssued, users.Count);
    }

    /// <summary>
    /// v4: Backfill SearchIndex for existing comments.
    /// </summary>
    private async Task BackfillCommentSearchIndexAsync()
    {
        var comments = await _commentCollection.Find(FilterDefinition<Comment>.Empty).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} comments to backfill search index for.", comments.Count);

        var migratedCount = 0;
        foreach (var comment in comments)
        {
            var searchIndex = Utils.GenerateSearchIndexFromContents(comment.Contents);
            if (comment.SearchIndex == searchIndex) continue;

            var filter = Builders<Comment>.Filter.Eq(x => x.Id, comment.Id);
            var update = Builders<Comment>.Update.Set(x => x.SearchIndex, searchIndex);

            await _commentCollection.UpdateOneAsync(filter, update);
            migratedCount++;
        }

        logger.LogInformation("[MIGRATION] Backfilled search index for {Count} comments.", migratedCount);
    }

    /// <summary>
    /// v5: Normalize legacy ExternalUrlContent thumbnail URLs to absolute http(s) URLs.
    /// Protocol-relative (//host/path) and path-relative (/favicon.ico) thumbnails crash
    /// BitmapImage on the Windows client, so each is resolved against its SourceUrl.
    /// </summary>
    private async Task NormalizeExternalUrlThumbnailsAsync()
    {
        await NormalizeExternalUrlThumbnailsAsync(_postCollection, "Posts");
        await NormalizeExternalUrlThumbnailsAsync(_publicPostCollection, "PublicPosts");
        await NormalizeExternalUrlThumbnailsAsync(_commentCollection, "Comments");
    }

    private async Task NormalizeExternalUrlThumbnailsAsync(IMongoCollection<Post> collection, string collectionName)
    {
        var posts = await collection.Find(FilterDefinition<Post>.Empty).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} posts to check in {Collection}.", posts.Count, collectionName);

        var migratedCount = 0;
        foreach (var post in posts)
        {
            if (NormalizeExternalUrlThumbnails(post.Contents) == 0) continue;

            var updateFilter = Builders<Post>.Filter.Eq(x => x.Id, post.Id);
            var update = Builders<Post>.Update.Set(x => x.Contents, post.Contents);
            await collection.UpdateOneAsync(updateFilter, update);
            migratedCount++;
        }

        logger.LogInformation("[MIGRATION] Normalized external URL thumbnails in {Count} {Collection}.", migratedCount, collectionName);
    }

    private async Task NormalizeExternalUrlThumbnailsAsync(IMongoCollection<Comment> collection, string collectionName)
    {
        var comments = await collection.Find(FilterDefinition<Comment>.Empty).ToListAsync();
        logger.LogInformation("[MIGRATION] Found {Count} comments to check in {Collection}.", comments.Count, collectionName);

        var migratedCount = 0;
        foreach (var comment in comments)
        {
            if (NormalizeExternalUrlThumbnails(comment.Contents) == 0) continue;

            var updateFilter = Builders<Comment>.Filter.Eq(x => x.Id, comment.Id);
            var update = Builders<Comment>.Update.Set(x => x.Contents, comment.Contents);
            await collection.UpdateOneAsync(updateFilter, update);
            migratedCount++;
        }

        logger.LogInformation("[MIGRATION] Normalized external URL thumbnails in {Count} {Collection}.", migratedCount, collectionName);
    }

    private static int NormalizeExternalUrlThumbnails(List<BaseContent> contents)
    {
        var changedCount = 0;
        foreach (var externalUrlContent in contents.OfType<ExternalUrlContent>())
        {
            var normalizedUrl = ResolveThumbnailUrl(externalUrlContent.ThumbnailImageUrl, externalUrlContent.SourceUrl);
            if (externalUrlContent.ThumbnailImageUrl == normalizedUrl) continue;
            externalUrlContent.ThumbnailImageUrl = normalizedUrl;
            changedCount++;
        }
        return changedCount;
    }

    private static string ResolveThumbnailUrl(string thumbnailImageUrl, string sourceUrl)
    {
        if (string.IsNullOrEmpty(thumbnailImageUrl)) return thumbnailImageUrl;

        // Protocol-relative URLs (//host/path) adopt the page's scheme. Checked before the
        // absolute parse because they would otherwise be parsed as file:// URIs.
        if (thumbnailImageUrl.StartsWith("//"))
        {
            if (Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceSchemeUri) && sourceSchemeUri.Scheme is "http" or "https") return sourceSchemeUri.Scheme + ":" + thumbnailImageUrl;
            return "https:" + thumbnailImageUrl;
        }

        if (Uri.TryCreate(thumbnailImageUrl, UriKind.Absolute, out var absoluteUri)) return absoluteUri.Scheme is "http" or "https" ? thumbnailImageUrl : "";
        if (!Uri.TryCreate(sourceUrl, UriKind.Absolute, out var sourceUri)) return "";
        if (sourceUri.Scheme is not ("http" or "https")) return "";

        try
        {
            var resolvedUri = new Uri(sourceUri, thumbnailImageUrl);
            return resolvedUri.Scheme is "http" or "https" ? resolvedUri.AbsoluteUri : "";
        }
        catch { return ""; }
    }
}
