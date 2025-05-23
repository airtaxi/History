using Amazon.Runtime.Internal.Transform;
using FirebaseAdmin.Messaging;
using History.ApiService.DataTypes;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Notification = History.Commons.DataTypes.Notification;

namespace History.ApiService.Services;

public class NotificationService(IMongoDatabase database, IServiceProvider serviceProvider) : INotificationService
{
    private readonly IMongoCollection<FirebaseToken> _firebaseTokenCollection = database.GetCollection<FirebaseToken>("FirebaseTokens");
    private readonly IMongoCollection<Notification> _notificationCollection = database.GetCollection<Notification>("Notifications");

    public async Task<Result<List<Notification>>> GetNotificationsAsync(string userId, string fromNotificationId = null, int limit = 30)
    {
        var filter = Builders<Notification>.Filter.AnyEq(n => n.Recipients, userId);

        if (fromNotificationId != null)
        {
            var fromNotification = await _notificationCollection.Find(f => f.Id == fromNotificationId).FirstOrDefaultAsync();
            if (fromNotification == null) return (ErrorType.NotFound, "알림을 찾을 수 없습니다");

            filter &= Builders<Notification>.Filter.Gt(n => n.CreatedAt, fromNotification.CreatedAt);
        }

        var notifications = await _notificationCollection.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Limit(limit)
            .ToListAsync();

        return notifications;
    }

    public async Task<Result> RegisterFirebaseTokenAsync(string userId, string firebaseToken)
    {
        var existingToken = await _firebaseTokenCollection.Find(f => f.UserId == userId && f.Token == firebaseToken).FirstOrDefaultAsync();
        if (existingToken != null)
        {
            var filter = Builders<FirebaseToken>.Filter.Eq(x => x.Id, existingToken.Id);
            var update = Builders<FirebaseToken>.Update.Set(x => x.CreatedAt, DateTime.UtcNow);
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
        if (notificationResult.IsFailure) notificationResult.CastFailure();

        var notification = notificationResult.Value;

        var recipients = notification.Recipients;
        var title = notification.Title;
        var body = notification.Body;
        var imageUrl = notification.ImageUrl;
        var data = notification.Data;

        recipients = recipients.Except([notification.UserId]).Distinct();

        if (!recipients.Any()) return Result.Success();

        while (true)
        {
            notification.Id = Guid.NewGuid().ToString("N");
            var existingNotification = await _notificationCollection.Find(f => f.Id == notification.Id).FirstOrDefaultAsync();
            if (existingNotification == null) break;
        }

        // Delete previous notifications
        await _notificationCollection.DeleteManyAsync(x => x.AssociatedId == associatedId && x.Type == type);

        // Insert new
        await _notificationCollection.InsertOneAsync(notification);

        return await SendFirebaseNotificationAsync(recipients, title, body, imageUrl, data);
    }

    public async Task<Result> SendFirebaseNotificationAsync(IEnumerable<string> recipientUserIds, string title, string body, string imageUrl, Dictionary<string, string> data)
    {
        var tokensResult = await GetFirebaseTokensFromUserIdsAsync(recipientUserIds);
        var tokens = tokensResult.Value;

        if (tokens.Count == 0) return Result.Success();

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
                    Visibility = NotificationVisibility.PRIVATE,
                },
            }
        };

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

    private async Task<Result<Notification>> GenerateNotificationAsync(NotificationType type, string associatedId)
    {
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

        if (type == NotificationType.Comment)
        {
            var postService = serviceProvider.GetRequiredService<IPostService>();
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();
            var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

            var commentResult = await commentService.GetCommentByIdAsync(associatedId);
            if (commentResult.IsFailure) return commentResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(commentResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            var commenterIdsResult = await commentService.GetCommenterUserIdsByPostIdAsync(commentResult.Value.PostId);
            if (commenterIdsResult.IsFailure) return commenterIdsResult.CastFailure<Notification>();

            var postResult = await postService.GetPostByIdAsync(commentResult.Value.PostId);
            if (postResult.IsFailure) return postResult.CastFailure<Notification>();

            var userFriendsIds = await friendshipService.GetUserFriendIdsAsync(commentResult.Value.UserId, commentResult.Value.UserId);
            if (userFriendsIds.IsFailure) return userFriendsIds.CastFailure<Notification>();

            core.Recipients = [.. commenterIdsResult.Value.Intersect(userFriendsIds.Value), postResult.Value.UserId];
            core.Recipients = core.Recipients.Distinct();
            core.UserId = userResult.Value.Id;

            if (userResult.Value.Id == postResult.Value.UserId) core.Recipients = core.Recipients.Except([userResult.Value.Id]);

            core.Title = $"{userResult.Value.Nickname}님이 게시글에 댓글을 달았습니다.";
            core.Body = await userService.GenerateTextPreviewFromContentsAsync(commentResult.Value.Contents);
            core.ImageUrl = Utils.GenerateThumbnailUrlFromContents(commentResult.Value.Contents);

            core.Data.Add("UserId", userResult.Value.Id);
            core.Data.Add("PostId", commentResult.Value.PostId);
            core.Data.Add("CommentId", commentResult.Value.Id);
        }
        else if (type == NotificationType.CommentMention)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var commentService = serviceProvider.GetRequiredService<ICommentService>();

            var commentResult = await commentService.GetCommentByIdAsync(associatedId);
            if (commentResult.IsFailure) return commentResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(commentResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            core.Recipients = commentResult.Value.Contents.OfType<ProfileContent>().Select(x => x.UserId).Distinct();
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
            if (commentLikeResult.IsFailure) return commentLikeResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(commentLikeResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            var commentResult = await commentService.GetCommentByIdAsync(commentLikeResult.Value.CommentId);
            if (commentResult.IsFailure) return commentResult.CastFailure<Notification>();

            core.Recipients = [commentResult.Value.UserId];
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
            if (postResult.IsFailure) return postResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            var parentPostResult = await postService.GetPostByIdAsync(postResult.Value.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure<Notification>();

            core.Recipients = [parentPostResult.Value.UserId];
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
            if (postResult.IsFailure) return postResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            var parentPostResult = await postService.GetPostByIdAsync(postResult.Value.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure<Notification>();

            core.Recipients = [parentPostResult.Value.UserId];
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
            if (postReactionResult.IsFailure) return postReactionResult.CastFailure<Notification>();

            var postResult = await postService.GetPostByIdAsync(postReactionResult.Value.PostId);
            if (postResult.IsFailure) return postResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(postReactionResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            core.Recipients = [postResult.Value.UserId];
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
            if (postResult.IsFailure) return postResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(postResult.Value.UserId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            core.Recipients = postResult.Value.Contents.OfType<ProfileContent>().Select(x => x.UserId).Distinct();
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
            if (friendshipResult.IsFailure) return friendshipResult.CastFailure<Notification>();

            var userResult = await userService.GetUserByIdAsync(friendshipResult.Value.FriendId);
            if (userResult.IsFailure) return userResult.CastFailure<Notification>();

            core.Recipients = [friendshipResult.Value.UserId];
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
        else throw new ArgumentException("Notification Type is not supported", type.ToString());

        if (core.Body.Length > 100) core.Body = core.Body[..100] + "...";

        return core;
    }
}
