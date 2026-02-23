using FirebaseAdmin.Messaging;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Driver;
using Notification = History.Commons.DataTypes.Notification;

namespace History.ApiService.Services;

public class NotificationService(IMongoDatabase database, IServiceProvider serviceProvider) : INotificationService
{
    private readonly IMongoCollection<FirebaseToken> _firebaseTokenCollection = database.GetCollection<FirebaseToken>("FirebaseTokens");
    private readonly IMongoCollection<Notification> _notificationCollection = database.GetCollection<Notification>("Notifications");
    private readonly IMongoCollection<NotificationRead> _notificationReadCollection = database.GetCollection<NotificationRead>("NotificationReads");
    private readonly IMongoCollection<IgnoredPost> _ignoredPostCollection = database.GetCollection<IgnoredPost>("IgnoredPosts");

    public async Task<Result<List<Notification>>> GetNotificationsAsync(string userId, string fromNotificationId = null, int limit = 30)
    {
        var filter = Builders<Notification>.Filter.AnyEq(n => n.Recipients, userId);

        if (fromNotificationId != null)
        {
            var fromNotification = await _notificationCollection.Find(f => f.Id == fromNotificationId).FirstOrDefaultAsync();
            if (fromNotification == null) return (ErrorType.NotFound, "알림을 찾을 수 없습니다");

            filter &= Builders<Notification>.Filter.Lt(n => n.CreatedAt, fromNotification.CreatedAt);
        }

        var notifications = await _notificationCollection.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        // Compute per-user read status from NotificationRead collection
        var notificationIds = notifications.Select(n => n.Id).ToList();
        var readNotificationIds = await _notificationReadCollection
            .Find(r => r.UserId == userId && notificationIds.Contains(r.NotificationId))
            .Project(r => r.NotificationId)
            .ToListAsync();

        var readSet = new HashSet<string>(readNotificationIds);
        foreach (var notification in notifications)
            notification.IsUnread = !readSet.Contains(notification.Id);

        return notifications;
    }

    public async Task<Result> RegisterFirebaseTokenAsync(string userId, string firebaseToken)
    {
        var existingToken = await _firebaseTokenCollection.Find(f => f.Token == firebaseToken).FirstOrDefaultAsync();
        if (existingToken != null)
        {
            var filter = Builders<FirebaseToken>.Filter.Eq(x => x.Id, existingToken.Id);
            var update = Builders<FirebaseToken>.Update
                .Set(x => x.CreatedAt, DateTime.UtcNow)
                .Set(x => x.UserId, userId);
            await _firebaseTokenCollection.UpdateOneAsync(filter, update);

            return Result.Success();
        }

        var newToken = new FirebaseToken
        {
            UserId = userId,
            Token = firebaseToken,
            CreatedAt = DateTime.UtcNow,
        };

        while(true)
        {
            newToken.Id = Guid.NewGuid().ToString("N");
            existingToken = await _firebaseTokenCollection.Find(f => f.Id == newToken.Id).FirstOrDefaultAsync();
            if (existingToken == null) break;
        }

        await _firebaseTokenCollection.InsertOneAsync(newToken);

        return Result.Success();
    }

    public async Task<Result> DeleteFirebaseTokensAsync(IEnumerable<string> firebaseTokens)
    {
        var filter = Builders<FirebaseToken>.Filter.In(f => f.Token, firebaseTokens);
        var result = await _firebaseTokenCollection.DeleteManyAsync(filter);

        return Result.Success();
    }

    public async Task<Result> DeleteNotificationsAsync(string filterKey, string filterValue, NotificationType? type = null)
    {
        var filter = Builders<Notification>.Filter.Eq(filterKey, filterValue);
        if (type != null) filter &= Builders<Notification>.Filter.Eq(n => n.Type, type);
        await _notificationCollection.DeleteManyAsync(filter);

        return Result.Success();
    }

    public async Task<Result> DeleteNotificationsAsync(string filterKey, IEnumerable<string> filterValues, NotificationType? type = null)
    {
        var filter = Builders<Notification>.Filter.In(filterKey, filterValues);
        if (type != null) filter &= Builders<Notification>.Filter.Eq(n => n.Type, type);
        await _notificationCollection.DeleteManyAsync(filter);

        return Result.Success();
    }

    public async Task<Result<List<string>>> GetFirebaseTokensFromUserIdAsync(string userId)
    {
        var filter = Builders<FirebaseToken>.Filter.Eq(f => f.UserId, userId);
        var tokens = await _firebaseTokenCollection.Find(filter).ToListAsync();
        return tokens.Select(x => x.Token).ToList();
    }

    public async Task<Result<List<string>>> GetFirebaseTokensFromUserIdsAsync(IEnumerable<string> userIds)
    {
        var filter = Builders<FirebaseToken>.Filter.In(f => f.UserId, userIds);
        var tokens = await _firebaseTokenCollection.Find(filter).ToListAsync();
        return tokens.Select(x => x.Token).ToList();
    }

    private const string AndroidChannelId = "com.airtaxi.history.push";
    public async Task<Result> SendNotificationsAsync(NotificationType type, string associatedId)
    {
        var notificationResult = await GenerateNotificationAsync(type, associatedId);
        if (notificationResult.IsFailure) return notificationResult.CastFailure();

        // Delete previous notifications
        var firstNotification = notificationResult.Value.FirstOrDefault();
        if (firstNotification == null) return Result.Success();

        var allRecipients = notificationResult.Value
            .SelectMany(n => n.Type != NotificationType.Birthday ? n.Recipients.Except([n.UserId]) : n.Recipients)
            .Distinct()
            .ToList();
        if ((type == NotificationType.Comment
            || type == NotificationType.CommentMention
            || type == NotificationType.Repost
            || type == NotificationType.PostReaction)
            && firstNotification.Data.TryGetValue("PostId", out var postId))
        {
            var filter = Builders<Notification>.Filter.Eq("Data.PostId", postId);
            if (type == NotificationType.Comment || type == NotificationType.CommentMention)
            {
                filter &= Builders<Notification>.Filter.Eq(n => n.Type, NotificationType.Comment)
                    | Builders<Notification>.Filter.Eq(n => n.Type, NotificationType.CommentMention);
            }
            else filter &= Builders<Notification>.Filter.Eq(n => n.Type, type);

            // Pull new notification recipients from old notifications instead of deleting entirely
            var update = Builders<Notification>.Update.PullAll(x => x.Recipients, allRecipients);
            await _notificationCollection.UpdateManyAsync(filter, update);
            await _notificationCollection.DeleteManyAsync(filter & Builders<Notification>.Filter.Size(n => n.Recipients, 0));
        }

        if (type == NotificationType.CommentLike && firstNotification.Data.TryGetValue("CommentId", out var commentId))
        {
            var filter = Builders<Notification>.Filter.Eq("Data.CommentId", commentId)
                & Builders<Notification>.Filter.Eq(n => n.Type, type);
            await _notificationCollection.DeleteManyAsync(filter);
        }

        await _notificationCollection.DeleteManyAsync(x => x.AssociatedId == associatedId && x.Type == type);

        // Send notifications
        foreach (var notification in notificationResult.Value)
        {
            var recipients = notification.Recipients;
            var pushNotificationRecipients = notification.PushNotificationRecipients;

            var title = notification.Title;
            var body = notification.Body;
            var imageUrl = notification.ImageUrl;
            var data = notification.Data;

            if (notification.Type != NotificationType.Birthday)
            {
                recipients = recipients.Except([notification.UserId]).Distinct();
                pushNotificationRecipients = pushNotificationRecipients.Except([notification.UserId]).Distinct();
            }

            if (!recipients.Any()) continue;

            while (true)
            {
                notification.Id = Guid.NewGuid().ToString("N");
                var existingNotification = await _notificationCollection.Find(f => f.Id == notification.Id).FirstOrDefaultAsync();
                if (existingNotification == null) break;
            }

            // Insert new
            await _notificationCollection.InsertOneAsync(notification);

            if (!notification.PushNotificationDisabled && pushNotificationRecipients.Any()) await SendFirebaseNotificationAsync(pushNotificationRecipients, title, body, imageUrl, data);
        }

        return Result.Success();
    }

    public async Task<Result> SendFirebaseNotificationAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data)
    {
        if (!Uri.IsWellFormedUriString(imageUrl, UriKind.Absolute)) imageUrl = null;

        var tokensResult = await GetFirebaseTokensFromUserIdsAsync(recipientUserIds);
        var tokens = tokensResult.Value;

        if (tokens.Count == 0) return Result.Success();

        string collapseKey = null;
        data.TryGetValue("Type", out var rawType);
        if (rawType != null && Enum.TryParse<NotificationType>(rawType, out var type))
        {
            if(data.TryGetValue("PostId", out var postId))
            {
                if (type == NotificationType.Comment) collapseKey = "comment_" + postId;
                else if (type == NotificationType.CommentMention) collapseKey = "comment_mention_" + postId;
                else if (type == NotificationType.CommentLike && data.TryGetValue("CommentId", out var commentId)) collapseKey = "comment_like_" + commentId;
                else if (type == NotificationType.Share) collapseKey = "share_" + postId;
                else if (type == NotificationType.Repost) collapseKey = "repost_" + postId;
                else if (type == NotificationType.PostReaction) collapseKey = "post_reaction_" + postId;
            }
            if (collapseKey != null) data["notification_id"] = collapseKey; // Use collapse key for Android and iOS to group notifications
        }

        var message = new MulticastMessage
        {
            Tokens = tokensResult.Value,
            Notification = new()
            {
                Title = title,
                Body = body,
                ImageUrl = imageUrl,
            },
            Data = data,
            Android = new AndroidConfig
            {
                Priority = Priority.High,
                Notification = new AndroidNotification
                {
                    ChannelId = AndroidChannelId,
                    ImageUrl = imageUrl,
                    NotificationCount = 1
                },
            }
        };

        if (collapseKey != null)
        {
            message.Android.Notification.Tag = collapseKey;
            if (message.Apns != null)
            {
                message.Apns = new ApnsConfig
                {
                    Headers = new Dictionary<string, string>
                    {
                        { "apns-collapse-id", collapseKey }
                    }
                };
            }
        }

        var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);

        var expiredTokens = new List<string>();
        for (int i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            if (result.Exception != null)
            {
                var errorCode = result.Exception.MessagingErrorCode;

                if (errorCode == MessagingErrorCode.Unregistered || errorCode == MessagingErrorCode.InvalidArgument)
                {
                    var expiredToken = tokens[i];
                    expiredTokens.Add(expiredToken);
                }
            }
        }

        if (expiredTokens.Count > 0) await DeleteFirebaseTokensAsync(expiredTokens);

        return Result.Success();
    }

    private async Task<Result<List<Notification>>> GenerateNotificationAsync(NotificationType type, string associatedId)
    {
        var notifications = new List<Notification>();
        var core = new Notification()
        {
            AssociatedId = associatedId,
            Type = type,
            Data = new Dictionary<string, string>
            {
                { "Type", type.ToString() }
            },
            CreatedAt = DateTime.UtcNow
        };
        notifications.Add(core);

        if (type == NotificationType.Comment)
        {
            var postService = serviceProvider.GetRequiredService<IPostService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

            var commentResult = await commentService.GetCommentByIdAsync(associatedId);
            if (commentResult.IsFailure) return commentResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(commentResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            var commenterIdsResult = await commentService.GetCommenterUserIdsByPostIdAsync(commentResult.Value.PostId);
            if (commenterIdsResult.IsFailure) return commenterIdsResult.CastFailure<List<Notification>>();

            var postResult = await postService.GetPostByIdAsync(commentResult.Value.PostId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var userFriendsIds = await friendshipService.GetUserFriendIdsAsync(commentResult.Value.UserId, commentResult.Value.UserId);
            if (userFriendsIds.IsFailure) return userFriendsIds.CastFailure<List<Notification>>();

            core.Recipients = commenterIdsResult.Value.Intersect(userFriendsIds.Value);

            var filterResult = await FilterRecipientsByAccessAsync(core.Recipients, postResult.Value);
            if (filterResult.IsFailure) return filterResult.CastFailure<List<Notification>>();
            core.Recipients = filterResult.Value.Except([postResult.Value.UserId]).Distinct();

            await SetPushNotificationRecipientsAsync(core, PushNotificationType.Comment);

            core.UserId = userResult.Value.Id;

            if (userResult.Value.Id == postResult.Value.UserId) core.Recipients = core.Recipients.Except([userResult.Value.Id]);

            var postAuthorResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (postAuthorResult.IsFailure) return postAuthorResult.CastFailure<List<Notification>>();

            core.Title = $"{userResult.Value.Nickname}님이 {postAuthorResult.Value.Nickname}님의 게시글에 댓글을 달았습니다.";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(commentResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(commentResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", commentResult.Value.PostId);
            core.Data.Add("CommentId", commentResult.Value.Id);

            var authorCore = core.Clone();
            authorCore.Recipients = [postResult.Value.UserId];
            await SetPushNotificationRecipientsAsync(authorCore, PushNotificationType.Comment);

            authorCore.Title = $"{userResult.Value.Nickname}님이 회원님의 게시글에 댓글을 달았습니다.";
            notifications.Add(authorCore);

            if (postResult.Value.ParentPostId != null)
            {
                var parentPostResult = await postService.GetPostByIdAsync(postResult.Value.ParentPostId);
                if (parentPostResult.IsSuccess)
                {
                    var parentPostAuthorResult = await userService.GetUserByIdAsync(parentPostResult.Value.UserId);
                    if (parentPostAuthorResult.IsSuccess)
                    {
                        var parentCore = core.Clone();
                        parentCore.Recipients = [parentPostResult.Value.UserId];
                        await SetPushNotificationRecipientsAsync(parentCore, PushNotificationType.SharedPostComment);

                        parentCore.Title = $"{userResult.Value.Nickname}님이 회원님의 공유 게시글에 댓글을 달았습니다.";
                        notifications.Add(parentCore);
                    }
                }
            }
        }
        else if (type == NotificationType.CommentMention)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();

            var commentResult = await commentService.GetCommentByIdAsync(associatedId);
            if (commentResult.IsFailure) return commentResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(commentResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            var postResult = await postService.GetPostByIdAsync(commentResult.Value.PostId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();


            core.Recipients = commentResult.Value.Contents.OfType<ProfileContent>().Select(x => x.UserId).Distinct();

            var filterResult = await FilterRecipientsByAccessAsync(core.Recipients, postResult.Value);
            if (filterResult.IsFailure) return filterResult.CastFailure<List<Notification>>();
            core.Recipients = filterResult.Value;

            await SetPushNotificationRecipientsAsync(core, PushNotificationType.CommentMention);

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 댓글에서 회원님을 언급하였습니다";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(commentResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(commentResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", commentResult.Value.PostId);
            core.Data.Add("CommentId", commentResult.Value.Id);
        }
        else if (type == NotificationType.CommentLike)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();

            var commentLikeResult = await commentService.GetCommentLikeByIdAsync(associatedId);
            if (commentLikeResult.IsFailure) return commentLikeResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(commentLikeResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            var commentResult = await commentService.GetCommentByIdAsync(commentLikeResult.Value.CommentId);
            if (commentResult.IsFailure) return commentResult.CastFailure<List<Notification>>();

            core.Recipients = [commentResult.Value.UserId];
            await SetPushNotificationRecipientsAsync(core, PushNotificationType.CommentLike);

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 내 댓글을 좋아합니다";
            core.Body = userService.GenerateTextPreviewFromContentsAsync(commentResult.Value.Contents).Result;
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(commentResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", commentResult.Value.PostId);
            core.Data.Add("CommentId", commentResult.Value.Id);
        }
        else if (type == NotificationType.Share)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            var parentPostResult = await postService.GetPostByIdAsync(postResult.Value.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure<List<Notification>>();

            core.Recipients = [parentPostResult.Value.UserId];
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 내 게시글을 공유했습니다";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(postResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(postResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", postResult.Value.Id);
            core.Data.Add("ParentPostId", parentPostResult.Value.Id);
        }
        else if (type == NotificationType.Repost)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            var parentPostResult = await postService.GetPostByIdAsync(postResult.Value.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure<List<Notification>>();

            core.Recipients = [parentPostResult.Value.UserId];
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 내 게시글을 리포스트했습니다";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(parentPostResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(parentPostResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", parentPostResult.Value.Id);
        }
        else if (type == NotificationType.PostReaction)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postReactionResult = await postService.GetPostReactionByIdAsync(associatedId);
            if (postReactionResult.IsFailure) return postReactionResult.CastFailure<List<Notification>>();

            var postResult = await postService.GetPostByIdAsync(postReactionResult.Value.PostId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(postReactionResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            core.Recipients = [postResult.Value.UserId];
            await SetPushNotificationRecipientsAsync(core, PushNotificationType.PostReaction);

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 내 게시글에 \"{postReactionResult.Value.Type.ToDisplayString()}\"를 남겼습니다.";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(postResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(postResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("ReactionType", postReactionResult.Value.Type.ToString());
            core.Data.Add("PostId", postResult.Value.Id);
        }
        else if (type == NotificationType.PostMention)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            core.Recipients = postResult.Value.Contents.OfType<ProfileContent>().Select(x => x.UserId).Distinct();

            var filterResult = await FilterRecipientsByAccessAsync(core.Recipients, postResult.Value);
            if (filterResult.IsFailure) return filterResult.CastFailure<List<Notification>>();
            core.Recipients = filterResult.Value;

            await SetPushNotificationRecipientsAsync(core, PushNotificationType.PostMention);

            core.UserId = userResult.Value.Id;

            core.Title = $"{userResult.Value.Nickname}님이 게시글에서 회원님을 언급했습니다";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(postResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(postResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", postResult.Value.Id);
        }
        else if (type == NotificationType.FriendRequest)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

            var friendshipResult = await friendshipService.GetFriendshipByIdAsync(associatedId);
            if (friendshipResult.IsFailure) return friendshipResult.CastFailure<List<Notification>>();

            var userResult = await userService.GetUserByIdAsync(friendshipResult.Value.FriendId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            core.Recipients = [friendshipResult.Value.UserId];
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = userResult.Value.Id;

            if (friendshipResult.Value.Status == FriendshipStatus.Waiting)
            {
                core.Title = $"{userResult.Value.Nickname}님이 회원님과 친구가 되고 싶어합니다.";
                core.Body = string.Empty;
                core.ImageUrl = userResult.Value.ProfileThumbnailMediaId != null ? Utils.GenerateMediaUri(userResult.Value.ProfileThumbnailMediaId) : null;
            }
            else if (friendshipResult.Value.Status == FriendshipStatus.Accepted)
            {
                core.Title = $"{userResult.Value.Nickname}님과 친구가 되었습니다.";
                core.Body = string.Empty;
                core.ImageUrl = userResult.Value.ProfileThumbnailMediaId != null ? Utils.GenerateMediaUri(userResult.Value.ProfileThumbnailMediaId) : null;
            }
            else throw new ArgumentException("Friendship status is not supported", friendshipResult.Value.Status.ToString());

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("FriendshipStatus", friendshipResult.Value.Status.ToString());
        }
        else if (type == NotificationType.Restriction)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var moderationService = serviceProvider.GetRequiredService<IModerationService>();

            var recordResult = await moderationService.GetModerationRecordByIdAsync(associatedId);
            if (recordResult.IsFailure) return recordResult.CastFailure<List<Notification>>();

            var moderatorResult = await userService.GetUserByIdAsync(recordResult.Value.ModeratorId);
            if (moderatorResult.IsFailure) return moderatorResult.CastFailure<List<Notification>>();

            core.Recipients = [recordResult.Value.UserId];
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = recordResult.Value.ModeratorId;

            core.Title = $"관리자 {moderatorResult.Value.Nickname}님이 회원님의 컨텐츠에 대해 [{recordResult.Value.ReportType.ToDisplayString()}]의 이유로 {recordResult.Value.RestrictionType.ToDisplayString()} 조치하였습니다.";
            core.Body = $"사유: {recordResult.Value.Reason}, 대상 컨텐츠 (글만 제공됨): " + await userService.GenerateTextPreviewFromContentsAsync(recordResult.Value.AssociatedContents);
            core.ImageUrl = moderatorResult.Value.ProfileThumbnailMediaId != null ? Utils.GenerateMediaUri(moderatorResult.Value.ProfileThumbnailMediaId) : null;

            core.Data.Add("RestrictedUserId", recordResult.Value.UserId);
            core.Data.Add("Body", core.Body);
            core.Data.Add("Reason", recordResult.Value.Reason);
            core.Data.Add("RestrictionType", recordResult.Value.RestrictionType.ToString());
            core.Data.Add("ReportType", recordResult.Value.ReportType.ToString());
        }
        else if (type == NotificationType.Report)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var reportService = serviceProvider.GetRequiredService<IReportService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();

            var recordResult = await reportService.GetReportRecordByIdAsync(associatedId);
            if (recordResult.IsFailure) return recordResult.CastFailure<List<Notification>>();

            var reporterResult = await userService.GetUserByIdAsync(recordResult.Value.ReporterId);
            if (reporterResult.IsFailure) return reporterResult.CastFailure<List<Notification>>();

            var moderatorUserIdsResult = await userService.GetModeratorIdsAsync();
            if (moderatorUserIdsResult.IsFailure) return moderatorUserIdsResult.CastFailure<List<Notification>>();

            core.Recipients = moderatorUserIdsResult.Value;
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = recordResult.Value.ReporterId;

            core.Data.Add("UserId", reporterResult.Value.Id);

            if (recordResult.Value.Target == ReportTarget.Post)
            {
                var postResult = await postService.GetPostByIdAsync(recordResult.Value.AssociatedId);
                if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

                var reportedUserResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
                if (reportedUserResult.IsFailure) return reportedUserResult.CastFailure<List<Notification>>();

                core.Title = $"{reporterResult.Value.Nickname}님이 {reportedUserResult.Value.Nickname}님의 {recordResult.Value.Target.ToDisplayString()}을 [{recordResult.Value.Type.ToDisplayString()}]의 이유로 신고하였습니다.";
                core.Body = await userService.GenerateTextPreviewFromContentsAsync(recordResult.Value.AssociatedContents, reportedUserResult.Value.Id);
                core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(recordResult.Value.AssociatedContents);

                core.Data.Add("PostId", postResult.Value.Id);
            }
            else if (recordResult.Value.Target == ReportTarget.Comment)
            {
                var commentResult = await commentService.GetCommentByIdAsync(recordResult.Value.AssociatedId);
                if (commentResult.IsFailure) return commentResult.CastFailure<List<Notification>>();

                var reportedUserResult = await userService.GetUserByIdAsync(commentResult.Value.UserId);
                if (reportedUserResult.IsFailure) return reportedUserResult.CastFailure<List<Notification>>();

                core.Title = $"{reporterResult.Value.Nickname}님이 {reportedUserResult.Value.Nickname}님의 {recordResult.Value.Target.ToDisplayString()}을 [{recordResult.Value.Type.ToDisplayString()}]의 이유로 신고하였습니다.";
                core.Body = await userService.GenerateTextPreviewFromContentsAsync(commentResult.Value.Contents, reportedUserResult.Value.Id);
                core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(commentResult.Value.Contents);

                core.Data.Add("PostId", commentResult.Value.PostId);
                core.Data.Add("CommentId", commentResult.Value.Id);
            }
            else return (ErrorType.BadRequest, "지원되지 않는 신고 대상입니다.");
        }
        else if (type == NotificationType.FavoriteFriendNewPost)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var favoritedFriendIdsResult = await friendshipService.GetFavoritedFriendIdsAsync(postResult.Value.UserId);
            if (favoritedFriendIdsResult.IsFailure) return favoritedFriendIdsResult.CastFailure<List<Notification>>();
            if (favoritedFriendIdsResult.Value.Count == 0) return (ErrorType.NotFound, "이 사용자를 관심 친구로 등록한 사용자가 없습니다.");

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Notification>>();

            core.Recipients = favoritedFriendIdsResult.Value;

            var filterResult = await FilterRecipientsByAccessAsync(core.Recipients, postResult.Value);
            if (filterResult.IsFailure) return filterResult.CastFailure<List<Notification>>();
            core.Recipients = filterResult.Value.Distinct();

            await SetPushNotificationRecipientsAsync(core, PushNotificationType.FavoriteFriendNewPost);

            core.UserId = postResult.Value.UserId;

            core.Title = $"관심 친구 {userResult.Value.Nickname}님이 새 게시글을 작성했습니다.";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(postResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(postResult.Value.Contents);

            core.Data.Add("PostId", postResult.Value.Id);
            core.Data.Add("UserId", postResult.Value.UserId);
        }
        else if (type == NotificationType.Birthday)
        {
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var postService = serviceProvider.GetRequiredService<IPostService>();

            var postResult = await postService.GetPostByIdAsync(associatedId);
            if (postResult.IsFailure) return postResult.CastFailure<List<Notification>>();

            var postAuthorResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (postAuthorResult.IsFailure) return postAuthorResult.CastFailure<List<Notification>>();

            var postAuthorFriendsResult = await friendshipService.GetUserFriendIdsAsync(postResult.Value.UserId, postResult.Value.UserId);
            if (postAuthorFriendsResult.IsFailure) return postAuthorFriendsResult.CastFailure<List<Notification>>();

            core.Recipients = postAuthorFriendsResult.Value.Except([postResult.Value.UserId]).Distinct();
            core.PushNotificationRecipients = core.Recipients;

            core.UserId = postResult.Value.UserId;

            core.Title = $"오늘은 회원님의 친구 {postAuthorResult.Value.Nickname}님의 생일입니다!";
            core.Body = $"생일 축하 메시지를 남겨주세요!";
            core.ImageUrl = "https://api.history.cenox.io/api/media/birthday";

            core.Data.Add("PostId", postResult.Value.Id);
            core.Data.Add("UserId", postResult.Value.UserId);
            core.PushNotificationDisabled = true;

            var authorCore = core.Clone();
            authorCore.Recipients = [postResult.Value.UserId];
            authorCore.PushNotificationRecipients = authorCore.Recipients;
            authorCore.Title = $"오늘은 회원님의 생일입니다! 🎉";
            authorCore.Body = $"회원님의 생일을 진심으로 축하드립니다! 다른 친구들이 남긴 축하 메시지를 확인해보세요!";
            notifications.Add(authorCore);
        }
        else if (type == NotificationType.Message)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var messageService = serviceProvider.GetRequiredService<IMessageService>();

            var messageResult = await messageService.GetMessageByIdAsync(associatedId);
            if (messageResult.IsFailure) return messageResult.CastFailure<List<Notification>>();

            var senderResult = await userService.GetUserByIdAsync(messageResult.Value.SenderId);
            if (senderResult.IsFailure) return senderResult.CastFailure<List<Notification>>();

            core.Recipients = [messageResult.Value.ReceiverId];
            await SetPushNotificationRecipientsAsync(core, PushNotificationType.Message);

            core.UserId = messageResult.Value.SenderId;

            core.Title = $"{senderResult.Value.Nickname}님이 쪽지를 보냈습니다.";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(messageResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(messageResult.Value.Contents);

            core.Data.Add("UserId", senderResult.Value.Id);
            core.Data.Add("MessageId", messageResult.Value.Id);
        }
        else return (ErrorType.BadRequest, "지원되지 않는 알림 유형입니다.");

		foreach (var notification in notifications)
        {
            if (notification.Body.Length > 100)
            {
                notification.Body = notification.Body[..100] + "...";
            }
        }

        return notifications;
    }

    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        var filter = Builders<FirebaseToken>.Filter.Eq(f => f.UserId, userId);
        await _firebaseTokenCollection.DeleteManyAsync(filter);

        var userFilter = Builders<Notification>.Filter.Eq(n => n.UserId, userId);
        await _notificationCollection.DeleteManyAsync(userFilter);

        var dataFIlter = Builders<Notification>.Filter.Eq("Data.UserId", userId);
        await _notificationCollection.DeleteManyAsync(dataFIlter);

        await _notificationReadCollection.DeleteManyAsync(r => r.UserId == userId);

        return Result.Success();
    }

    public async Task<Result> MarkNotificationsAsReadAsync(string userId, IEnumerable<string> notificationIds)
    {
        var validIds = await _notificationCollection
            .Find(Builders<Notification>.Filter.In(n => n.Id, notificationIds)
                & Builders<Notification>.Filter.AnyEq(n => n.Recipients, userId))
            .Project(n => n.Id)
            .ToListAsync();

        await InsertNotificationReadsAsync(userId, validIds);
        return Result.Success();
    }

    public async Task<Result> MarkAllNotificationsAsReadAsync(string userId)
    {
        var notificationIds = await _notificationCollection
            .Find(Builders<Notification>.Filter.AnyEq(n => n.Recipients, userId))
            .Project(n => n.Id)
            .ToListAsync();

        await InsertNotificationReadsAsync(userId, notificationIds);
        return Result.Success();
    }

    public async Task<Result> MarkNotificationsByDataAsReadAsync(string userId, string dataKey, string dataValue, NotificationType? type = null)
    {
        var filter = Builders<Notification>.Filter.AnyEq(n => n.Recipients, userId)
            & Builders<Notification>.Filter.Eq($"Data.{dataKey}", dataValue);
        if (type.HasValue) filter &= Builders<Notification>.Filter.Eq(n => n.Type, type.Value);

        var notificationIds = await _notificationCollection
            .Find(filter)
            .Project(n => n.Id)
            .ToListAsync();

        await InsertNotificationReadsAsync(userId, notificationIds);
        return Result.Success();
    }

    /// <summary>
    /// Bulk upsert NotificationRead records. Duplicates are handled gracefully via upsert.
    /// </summary>
    private async Task InsertNotificationReadsAsync(string userId, IEnumerable<string> notificationIds)
    {
        var operations = notificationIds.Select(notificationId =>
        {
            var id = $"{notificationId}_{userId}";
            var filter = Builders<NotificationRead>.Filter.Eq(r => r.Id, id);
            var update = Builders<NotificationRead>.Update
                .SetOnInsert(r => r.NotificationId, notificationId)
                .SetOnInsert(r => r.UserId, userId)
                .SetOnInsert(r => r.ReadAt, DateTime.UtcNow);
            return new UpdateOneModel<NotificationRead>(filter, update) { IsUpsert = true };
        }).ToList();

        if (operations.Count > 0)
            await _notificationReadCollection.BulkWriteAsync(operations, new BulkWriteOptions { IsOrdered = false });
    }

    private async Task SetPushNotificationRecipientsAsync(Notification notification, PushNotificationType pushNotificationType)
    {
        if (notification.PushNotificationDisabled)
        {
            notification.PushNotificationRecipients = [];
            return;
        }

        Console.WriteLine($"[B]SPNRA: {string.Join(",", notification.Recipients)}");
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var pushNotificationRecipientsResult = await userService.FilterPushNotificationPermissionsAsync(notification.UserId, notification.Recipients, pushNotificationType);
        if (pushNotificationRecipientsResult.IsFailure) notification.PushNotificationRecipients = [];
        else
        {
            Console.WriteLine($"SPNRA: {string.Join(",", pushNotificationRecipientsResult.Value ?? [])}");
            notification.PushNotificationRecipients = pushNotificationRecipientsResult.Value;
        }

    }


    private async Task<Result<List<string>>> FilterRecipientsByAccessAsync(IEnumerable<string> recipients, Post post)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Filter out users who have ignored this post
        var ignoredUserIds = await _ignoredPostCollection
            .Find(i => i.PostId == post.Id)
            .Project(i => i.UserId)
            .ToListAsync();
        recipients = recipients.Except(ignoredUserIds);

        if (post.DiscoveryOption == DiscoveryOption.Everyone) return recipients.ToList();
        else if (post.DiscoveryOption == DiscoveryOption.FriendsOfFriends)
        {
            var result = await friendshipService.GetFriendsOfFriendIdsAsync(post.UserId);
            if (result.IsFailure) return result.CastFailure<List<string>>();

            result.Value.Add(post.UserId); // Include the post author
            return recipients.Intersect(result.Value).Distinct().ToList();
        }
        else if (post.DiscoveryOption == DiscoveryOption.Friends)
        {
            var result = await friendshipService.GetUserFriendIdsAsync(post.UserId, post.UserId);
            if (result.IsFailure) return result;

            result.Value.Add(post.UserId); // Include the post author
            return recipients.Intersect(result.Value).Distinct().ToList();
        }
        else if (post.DiscoveryOption == DiscoveryOption.SelectedUsers) return recipients.Intersect(post.DiscoveryOptionSelectedUserIds).ToList();
        else if (post.DiscoveryOption == DiscoveryOption.UnselectedUsers) return recipients.Except(post.DiscoveryOptionSelectedUserIds).ToList();
        else if (post.DiscoveryOption == DiscoveryOption.OnlyMe) return new List<string> { post.UserId };
        else return (ErrorType.BadRequest, "Invalid Discovery Option");
    }
}
