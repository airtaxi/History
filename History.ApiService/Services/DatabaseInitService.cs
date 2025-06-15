
using History.Commons.DataTypes;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class DatabaseInitService(IMongoDatabase database, ILogger<DatabaseInitService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing database...");

        var userCollection = database.GetCollection<User>("Users");
        var postCollection = database.GetCollection<Post>("Posts");
        var publicPostCollection = database.GetCollection<Post>("PublicPosts");
        var friendshipCollection = database.GetCollection<Friendship>("Friendships");
        var commentCollection = database.GetCollection<Comment>("Comments");
        var mediaCollection = database.GetCollection<Media>("Medias");
        var refreshTokenCollection = database.GetCollection<RefreshToken>("RefreshTokens");
        var firebaseTokenCollection = database.GetCollection<FirebaseToken>("FirebaseTokens");
        var notificationCollection = database.GetCollection<Notification>("Notifications");

        // Create indexes
        logger.LogInformation("Creating indexes...");

        logger.LogInformation("Creating indexes for User collection...");
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Nickname)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Post collection...");
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.ParentPostId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.SearchIndex)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.DiscoveryOption)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.DiscoveryOptionSelectedUserIds)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for PublicPost collection...");
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.ParentPostId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.SearchIndex)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Friendship collection...");
        await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.FriendId)), cancellationToken: cancellationToken);
        await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.Status)), cancellationToken: cancellationToken);
        await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Comment collection...");
        await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Ascending(x => x.PostId)), cancellationToken: cancellationToken);
        await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Media collection...");
        await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.FileName)), cancellationToken: cancellationToken);
        await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.AssociatedId)), cancellationToken: cancellationToken);
        await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.BucketType)), cancellationToken: cancellationToken);
        await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for RefreshToken collection...");
        await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(x => x.Token)), cancellationToken: cancellationToken);
        await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for FirebaseToken collection...");
        await firebaseTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<FirebaseToken>(Builders<FirebaseToken>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await firebaseTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<FirebaseToken>(Builders<FirebaseToken>.IndexKeys.Ascending(x => x.Token)), cancellationToken: cancellationToken);
        await firebaseTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<FirebaseToken>(Builders<FirebaseToken>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Notification collection...");
        await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(x => x.Recipients)), cancellationToken: cancellationToken);
        await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(x => x.Type)), cancellationToken: cancellationToken);
        await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending(x => x.AssociatedId)), cancellationToken: cancellationToken);
        await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Ascending("Data.$**")), cancellationToken: cancellationToken);
        await notificationCollection.Indexes.CreateOneAsync(new CreateIndexModel<Notification>(Builders<Notification>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for ModerationRecord collection...");
        var moderationRecordCollection = database.GetCollection<ModerationRecord>("ModerationRecords");
        await moderationRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ModerationRecord>(Builders<ModerationRecord>.IndexKeys.Ascending(x => x.RestrictionType)), cancellationToken: cancellationToken);
        await moderationRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ModerationRecord>(Builders<ModerationRecord>.IndexKeys.Ascending(x => x.AssociatedId)), cancellationToken: cancellationToken);
        await moderationRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ModerationRecord>(Builders<ModerationRecord>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await moderationRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ModerationRecord>(Builders<ModerationRecord>.IndexKeys.Ascending(x => x.ModeratorId)), cancellationToken: cancellationToken);
        await moderationRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ModerationRecord>(Builders<ModerationRecord>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for ReportRecord collection...");
        var reportRecordCollection = database.GetCollection<ReportRecord>("ReportRecords");
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Ascending(x => x.Target)), cancellationToken: cancellationToken);
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Ascending(x => x.Type)), cancellationToken: cancellationToken);
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Ascending(x => x.AssociatedId)), cancellationToken: cancellationToken);
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Ascending(x => x.ReporterId)), cancellationToken: cancellationToken);
        await reportRecordCollection.Indexes.CreateOneAsync(new CreateIndexModel<ReportRecord>(Builders<ReportRecord>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Indexes created successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
