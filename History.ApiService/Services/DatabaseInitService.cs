using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class DatabaseInitService(IMongoDatabase database, ILogger<DatabaseInitService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Initializing database...");

        var userCollection = database.GetCollection<User>("Users");
        var userMemoCollection = database.GetCollection<UserMemo>("UserMemos");
        var postCollection = database.GetCollection<Post>("Posts");
        var publicPostCollection = database.GetCollection<Post>("PublicPosts");
        var friendshipCollection = database.GetCollection<Friendship>("Friendships");
        var commentCollection = database.GetCollection<Comment>("Comments");
        var mediaCollection = database.GetCollection<Media>("Medias");
        var refreshTokenCollection = database.GetCollection<RefreshToken>("RefreshTokens");
        var firebaseTokenCollection = database.GetCollection<FirebaseToken>("FirebaseTokens");
        var notificationCollection = database.GetCollection<Notification>("Notifications");

        // Poll / Sticker
        var pollVoteCollection = database.GetCollection<PollVote>("PollVotes");
        var stickerCollection = database.GetCollection<Sticker>("Stickers");
        var stickerAssetCollection = database.GetCollection<StickerAsset>("StickerAssets");
        var stickerSubscriptionCollection = database.GetCollection<StickerSubscription>("StickerSubscriptions");
        var recentStickerUsageCollection = database.GetCollection<RecentStickerUsage>("RecentStickerUsages");

        // Bookmarks
        var bookmarkedPostCollection = database.GetCollection<BookmarkedPost>("BookmarkedPosts");

        // Muted posts (post notification mutes)
        var mutedPostCollection = database.GetCollection<MutedPost>("MutedPosts");

        // Invite Codes
        var inviteCodeCollection = database.GetCollection<InviteCode>("InviteCodes");
        var inviteCodeRequestCollection = database.GetCollection<InviteCodeRequest>("InviteCodeRequests");

        // Create indexes
        logger.LogInformation("Creating indexes...");

        logger.LogInformation("Creating indexes for User collection...");
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Nickname)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Handle)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Email)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Rank)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.SocialService)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.FriendListDiscoveryOption)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.LastUsedPostDiscoveryOption)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.AllowSearch)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.PinnedPostId)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.MessageReceivingPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.CommentPushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.CommentMentionPushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.CommentLikePushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.SharedPostCommentPushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.PostReactionPushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.PostMentionPushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.MessagePushNotificationPermission)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.IsFavoriteFriendNewPostPushNotificationEnabled)), cancellationToken: cancellationToken);
        await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for UserMemo collection...");
        await userMemoCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserMemo>(Builders<UserMemo>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await userMemoCollection.Indexes.CreateOneAsync(new CreateIndexModel<UserMemo>(Builders<UserMemo>.IndexKeys.Ascending(x => x.RegisteredBy)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Post collection...");
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.ParentPostId)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.DiscoveryOption)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.DiscoveryOptionSelectedUserIds)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.CommentPermission)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Text(x => x.SearchIndex)), cancellationToken: cancellationToken);
        await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for PublicPost collection...");
        await publicPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await publicPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.ParentPostId)), cancellationToken: cancellationToken);
        await publicPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.SearchIndex)), cancellationToken: cancellationToken);
        await publicPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

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

        logger.LogInformation("Creating indexes for NotificationRead collection...");
        var notificationReadCollection = database.GetCollection<NotificationRead>("NotificationReads");
        await notificationReadCollection.Indexes.CreateOneAsync(new CreateIndexModel<NotificationRead>(Builders<NotificationRead>.IndexKeys.Ascending(x => x.NotificationId)), cancellationToken: cancellationToken);
        await notificationReadCollection.Indexes.CreateOneAsync(new CreateIndexModel<NotificationRead>(Builders<NotificationRead>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);

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

        logger.LogInformation("Creating indexes for PollVote collection...");
        await pollVoteCollection.Indexes.CreateOneAsync(new CreateIndexModel<PollVote>(Builders<PollVote>.IndexKeys.Ascending(x => x.PollId)), cancellationToken: cancellationToken);
        await pollVoteCollection.Indexes.CreateOneAsync(new CreateIndexModel<PollVote>(Builders<PollVote>.IndexKeys.Ascending(x => x.PostId)), cancellationToken: cancellationToken);
        await pollVoteCollection.Indexes.CreateOneAsync(new CreateIndexModel<PollVote>(Builders<PollVote>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await pollVoteCollection.Indexes.CreateOneAsync(new CreateIndexModel<PollVote>(Builders<PollVote>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);
        await pollVoteCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<PollVote>(
                Builders<PollVote>.IndexKeys.Combine(
                    Builders<PollVote>.IndexKeys.Ascending(x => x.PollId),
                    Builders<PollVote>.IndexKeys.Ascending(x => x.UserId)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for Sticker collection...");
        await stickerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sticker>(Builders<Sticker>.IndexKeys.Ascending(x => x.AuthorId)), cancellationToken: cancellationToken);
        await stickerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sticker>(Builders<Sticker>.IndexKeys.Ascending(x => x.Category)), cancellationToken: cancellationToken);
        await stickerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sticker>(Builders<Sticker>.IndexKeys.Ascending(x => x.IsPrivate)), cancellationToken: cancellationToken);
        await stickerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sticker>(Builders<Sticker>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);
        await stickerCollection.Indexes.CreateOneAsync(new CreateIndexModel<Sticker>(Builders<Sticker>.IndexKeys.Text(x => x.Name)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for StickerAsset collection...");
        await stickerAssetCollection.Indexes.CreateOneAsync(new CreateIndexModel<StickerAsset>(Builders<StickerAsset>.IndexKeys.Ascending(x => x.StickerId)), cancellationToken: cancellationToken);
        await stickerAssetCollection.Indexes.CreateOneAsync(new CreateIndexModel<StickerAsset>(Builders<StickerAsset>.IndexKeys.Ascending(x => x.MediaId)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for StickerSubscription collection...");
        await stickerSubscriptionCollection.Indexes.CreateOneAsync(new CreateIndexModel<StickerSubscription>(Builders<StickerSubscription>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await stickerSubscriptionCollection.Indexes.CreateOneAsync(new CreateIndexModel<StickerSubscription>(Builders<StickerSubscription>.IndexKeys.Ascending(x => x.StickerId)), cancellationToken: cancellationToken);
        await stickerSubscriptionCollection.Indexes.CreateOneAsync(new CreateIndexModel<StickerSubscription>(Builders<StickerSubscription>.IndexKeys.Descending(x => x.SubscribedAt)), cancellationToken: cancellationToken);
        await stickerSubscriptionCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<StickerSubscription>(
                Builders<StickerSubscription>.IndexKeys.Combine(
                    Builders<StickerSubscription>.IndexKeys.Ascending(x => x.UserId),
                    Builders<StickerSubscription>.IndexKeys.Ascending(x => x.StickerId)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for RecentStickerUsage collection...");
        await recentStickerUsageCollection.Indexes.CreateOneAsync(new CreateIndexModel<RecentStickerUsage>(Builders<RecentStickerUsage>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await recentStickerUsageCollection.Indexes.CreateOneAsync(new CreateIndexModel<RecentStickerUsage>(Builders<RecentStickerUsage>.IndexKeys.Ascending(x => x.StickerId)), cancellationToken: cancellationToken);
        await recentStickerUsageCollection.Indexes.CreateOneAsync(new CreateIndexModel<RecentStickerUsage>(Builders<RecentStickerUsage>.IndexKeys.Ascending(x => x.StickerAssetId)), cancellationToken: cancellationToken);
        await recentStickerUsageCollection.Indexes.CreateOneAsync(new CreateIndexModel<RecentStickerUsage>(Builders<RecentStickerUsage>.IndexKeys.Descending(x => x.LastUsedAt)), cancellationToken: cancellationToken);
        await recentStickerUsageCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<RecentStickerUsage>(
                Builders<RecentStickerUsage>.IndexKeys.Combine(
                    Builders<RecentStickerUsage>.IndexKeys.Ascending(x => x.UserId),
                    Builders<RecentStickerUsage>.IndexKeys.Ascending(x => x.StickerAssetId)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for BookmarkedPost collection...");
        await bookmarkedPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<BookmarkedPost>(Builders<BookmarkedPost>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
        await bookmarkedPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<BookmarkedPost>(Builders<BookmarkedPost>.IndexKeys.Ascending(x => x.PostId)), cancellationToken: cancellationToken);
        await bookmarkedPostCollection.Indexes.CreateOneAsync(new CreateIndexModel<BookmarkedPost>(Builders<BookmarkedPost>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);
        await bookmarkedPostCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<BookmarkedPost>(
                Builders<BookmarkedPost>.IndexKeys.Combine(
                    Builders<BookmarkedPost>.IndexKeys.Ascending(x => x.UserId),
                    Builders<BookmarkedPost>.IndexKeys.Ascending(x => x.PostId)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for MutedPost collection...");
        await mutedPostCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<MutedPost>(
                Builders<MutedPost>.IndexKeys.Combine(
                    Builders<MutedPost>.IndexKeys.Ascending(x => x.PostId),
                    Builders<MutedPost>.IndexKeys.Ascending(x => x.UserId)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for DailyFortuneRecord collection...");
        var dailyFortuneRecordCollection = database.GetCollection<DailyFortuneRecord>("DailyFortuneRecords");
        await dailyFortuneRecordCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<DailyFortuneRecord>(
                Builders<DailyFortuneRecord>.IndexKeys.Combine(
                    Builders<DailyFortuneRecord>.IndexKeys.Ascending(x => x.UserId),
                    Builders<DailyFortuneRecord>.IndexKeys.Ascending(x => x.Date)),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for InviteCode collection...");
        await inviteCodeCollection.Indexes.CreateOneAsync(
            new CreateIndexModel<InviteCode>(Builders<InviteCode>.IndexKeys.Ascending(x => x.Code), new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);
        await inviteCodeCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCode>(Builders<InviteCode>.IndexKeys.Ascending(x => x.OwnerId)), cancellationToken: cancellationToken);
        await inviteCodeCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCode>(Builders<InviteCode>.IndexKeys.Ascending(x => x.IsActive)), cancellationToken: cancellationToken);
        await inviteCodeCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCode>(Builders<InviteCode>.IndexKeys.Ascending(x => x.UsedByUserId)), cancellationToken: cancellationToken);
        await inviteCodeCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCode>(Builders<InviteCode>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

        logger.LogInformation("Creating indexes for InviteCodeRequest collection...");
        await inviteCodeRequestCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCodeRequest>(Builders<InviteCodeRequest>.IndexKeys.Ascending(x => x.RequesterId)), cancellationToken: cancellationToken);
        await inviteCodeRequestCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCodeRequest>(Builders<InviteCodeRequest>.IndexKeys.Ascending(x => x.Status)), cancellationToken: cancellationToken);
        await inviteCodeRequestCollection.Indexes.CreateOneAsync(new CreateIndexModel<InviteCodeRequest>(Builders<InviteCodeRequest>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);
        // Partial unique index: a user may have at most one Pending request at a time
        var partialIndexCommand = MongoDB.Bson.BsonDocument.Parse("{ 'createIndexes': 'InviteCodeRequests', 'indexes': [{ 'key': { 'RequesterId': 1 }, 'name': 'UniquePendingRequesterId', 'unique': true, 'partialFilterExpression': { 'Status': 'Pending' } }] }");
        await database.RunCommandAsync<MongoDB.Bson.BsonDocument>(partialIndexCommand, cancellationToken: cancellationToken);

        logger.LogInformation("Indexes created successfully.");

        // Use below code if migration is needed in future versions.

        //// Migrate Notification.IsUnread to per-user NotificationRead collection
        //// Run once after deploying the NotificationRead refactoring, then comment out.
        //logger.LogInformation("Migrating Notification.IsUnread to NotificationRead collection...");
        //var rawNotificationCollection = database.GetCollection<MongoDB.Bson.BsonDocument>("Notifications");
        //var readNotifications = await rawNotificationCollection
        //    .Find(new MongoDB.Bson.BsonDocument("IsUnread", false))
        //    .ToListAsync(cancellationToken);
        //
        //var bulkOps = new List<WriteModel<NotificationRead>>();
        //foreach (var doc in readNotifications)
        //{
        //    var notifId = doc["_id"].AsString;
        //    var recipients = doc.Contains("Recipients") ? doc["Recipients"].AsBsonArray.Select(x => x.AsString) : [];
        //    foreach (var recipientId in recipients)
        //    {
        //        var compositeId = $"{notifId}_{recipientId}";
        //        var filter = Builders<NotificationRead>.Filter.Eq(r => r.Id, compositeId);
        //        var update = Builders<NotificationRead>.Update
        //            .SetOnInsert(r => r.NotificationId, notifId)
        //            .SetOnInsert(r => r.UserId, recipientId)
        //            .SetOnInsert(r => r.ReadAt, DateTime.UtcNow);
        //        bulkOps.Add(new UpdateOneModel<NotificationRead>(filter, update) { IsUpsert = true });
        //    }
        //}
        //if (bulkOps.Count > 0)
        //{
        //    await notificationReadCollection.BulkWriteAsync(bulkOps, new BulkWriteOptions { IsOrdered = false }, cancellationToken);
        //    logger.LogInformation("Migrated {Count} NotificationRead records.", bulkOps.Count);
        //}
        //
        //// Remove legacy IsUnread field from Notification documents
        //var unsetUpdate = Builders<MongoDB.Bson.BsonDocument>.Update.Unset("IsUnread");
        //await rawNotificationCollection.UpdateManyAsync(MongoDB.Bson.BsonDocument.Parse("{}"), unsetUpdate, cancellationToken: cancellationToken);
        //logger.LogInformation("Removed legacy IsUnread field from Notification documents.");

        //// Migrate users with missing permission fields to default values
        //logger.LogInformation("Migrating users with missing permission fields...");

        //var permissionFields = new (string FieldName, object DefaultValue)[]
        //{
        //    (nameof(User.MessageReceivingPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.CommentPushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.CommentMentionPushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.CommentLikePushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.SharedPostCommentPushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.PostReactionPushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.PostMentionPushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.MessagePushNotificationPermission), Commons.Enums.AccessPermission.Everyone),
        //    (nameof(User.IsFavoriteFriendNewPostPushNotificationEnabled), true)
        //};

        //// Find users with any missing permission field
        //var filterBuilder = Builders<User>.Filter;
        //var missingFieldFilters = permissionFields.Select(f => filterBuilder.Exists(f.FieldName, false)).ToArray();
        //var anyMissingFieldFilter = filterBuilder.Or(missingFieldFilters);

        //// Build update definition for all missing fields
        //var updateBuilder = Builders<User>.Update;
        //var updates = permissionFields.Select(f => updateBuilder.SetOnInsert(f.FieldName, f.DefaultValue)).ToArray();
        //var combinedUpdate = updateBuilder.Combine(updates);

        //// Update all fields at once for each user with any missing field
        //foreach (var (fieldName, defaultValue) in permissionFields)
        //{
        //    var filter = filterBuilder.Exists(fieldName, false);
        //    var update = updateBuilder.Set(fieldName, defaultValue);
        //    var result = await userCollection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        //    if (result.ModifiedCount > 0)
        //    {
        //        logger.LogInformation("Set {FieldName} for {Count} users.", fieldName, result.ModifiedCount);
        //    }
        //}

        //// Count unique users that were updated
        //var usersWithMissingFields = await userCollection.CountDocumentsAsync(anyMissingFieldFilter, cancellationToken: cancellationToken);
        //logger.LogInformation("Migration completed. Total users with missing permission fields: {TotalCount}", usersWithMissingFields);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
