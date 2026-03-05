using History.ApiService.DataTypes;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class MigrationService(IMongoDatabase database, ILogger<MigrationService> logger) : IHostedService
{
    private readonly IMongoCollection<MigrationRecord> _migrations = database.GetCollection<MigrationRecord>("Migrations");
    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<Post> _publicPostCollection = database.GetCollection<Post>("PublicPosts");

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
}
