using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class PostService(IMongoDatabase database, IMediaService mediaService, INotificationService notificationService, IServiceProvider serviceProvider) : IPostService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");
    private readonly IMongoCollection<CommentLike> _commentLikeCollection = database.GetCollection<CommentLike>("CommentLikes");

    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<Post> _publicPostCollection = database.GetCollection<Post>("PublicPosts");
    private readonly IMongoCollection<IgnoredPost> _ignoredPostCollection = database.GetCollection<IgnoredPost>("IgnoredPosts");
    private readonly IMongoCollection<PostReaction> _postReactionCollection = database.GetCollection<PostReaction>("PostReactions");

    /// <inheritdoc />
    public async Task<Result<Post>> GetPostByIdAsync(string postId)
    {
        var post = await _postCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();

        if (post == null) return (ErrorType.NotFound, "게시글을 찾을 수 없습니다.");
        else return post;
    }
    public async Task<Result<PostReaction>> GetPostReactionByIdAsync(string postReactionId)
    {
        var postReaction = await _postReactionCollection.Find(p => p.Id == postReactionId).FirstOrDefaultAsync();

        if (postReaction == null) return (ErrorType.NotFound, "게시글 반응을 찾을 수 없습니다.");
        else return postReaction;
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetUserPostsAsync(string userId, string requesterId = null, string fromPostId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Check relationship between requester and target user
        bool isSelf = requesterId == userId;
        bool areFriends = false;
        bool areFriendsOfFriends = false;

        var bannedUserIds = new List<string>();
        var ignoredPostIds = new List<string>();

        // Only check relationships and apply ban filter if requesterId is provided (logged in user)
        if (!string.IsNullOrEmpty(requesterId) && !isSelf)
        {
            // Check if they are friends
            areFriends = await friendshipService.AreFriendsAsync(requesterId, userId);

            // If not friends, check if they are friends of friends
            if (!areFriends)
            {
                areFriendsOfFriends = await friendshipService.AreFriendsOfFriendsAsync(requesterId, userId);
            }

            // Get banned user IDs
            bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);

            ignoredPostIds.AddRange(await _ignoredPostCollection
                .Find(i => i.UserId == requesterId)
                .Project(i => i.PostId)
                .ToListAsync());
        }

        // Base filter: posts from the target user
        var filter = Builders<Post>.Filter.Eq(p => p.UserId, userId);

        // Add visibility filters based on privacy settings
        if (!isSelf)
        {
            // Always include public posts
            var visibilityFilter = new List<FilterDefinition<Post>>
            {
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Everyone)
            };

            // Friends or FriendsOfFriends (if friends)
            if (!string.IsNullOrEmpty(requesterId) && areFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    )
                );
            }

            // FriendsOfFriends (if not friends but are FoF)
            if (!string.IsNullOrEmpty(requesterId) && areFriendsOfFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                );
            }

            // SelectedUsers
            if (!string.IsNullOrEmpty(requesterId) && areFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                    )
                );

                // UnselectedUsers (NOT in the excluded list)
                visibilityFilter.Add(
                    Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.UnselectedUsers),
                        Builders<Post>.Filter.AnyNe(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                    )
                );
            }

            // Combine all filters
            filter &= Builders<Post>.Filter.Or(visibilityFilter);

            if (bannedUserIds.Count > 0) filter &= Builders<Post>.Filter.Nin(p => p.UserId, bannedUserIds);
            if (ignoredPostIds.Count > 0) filter &= Builders<Post>.Filter.Nin(p => p.Id, ignoredPostIds);
        }

        // Add pagination filter
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _postCollection.Find(p => p.Id == fromPostId).FirstOrDefaultAsync();
            if (fromPost != null)
            {
                filter &= Builders<Post>.Filter.Lt(p => p.CreatedAt, fromPost.CreatedAt);
            }
        }

        // Ensure the post is not a repost
        filter &= Builders<Post>.Filter.Eq(p => p.IsRepost, false);

        // For reservation posts.
        filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, DateTime.UtcNow);

        // Retrieve and return posts sorted by creation time (newest first)
        var result = await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();

        if (fromPostId == null)
        {
            var userService = serviceProvider.GetRequiredService<IUserService>();
            var userResult = await userService.GetUserByIdAsync(userId);
            if (userResult.IsFailure) return userResult.CastFailure<List<Post>>();

            var pinnedPostid = userResult.Value.PinnedPostId;
            if (string.IsNullOrEmpty(pinnedPostid)) return result;

            var pinnedPost = await _postCollection.Find(p => p.UserId == userId && p.Id == pinnedPostid).FirstOrDefaultAsync();
            if (pinnedPost != null)
            {
                var access = await CheckAccessAsync(pinnedPost, requesterId);
                if (access.IsFailure) return result;

                var existingPinnedPost = result.FirstOrDefault(p => p.Id == pinnedPost.Id);
                if (existingPinnedPost != null) result.Remove(existingPinnedPost);
                result.Insert(0, pinnedPost);
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetPublicPostsAsync(string requesterId, string fromPostId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Get IDs of user's friends and add the user's own ID
        var bannedUserIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);
        var bannedUserIds = bannedUserIdsResult.Value;

        var ignoredPostIds = await _ignoredPostCollection
                .Find(i => i.UserId == requesterId)
                .Project(i => i.PostId)
                .ToListAsync();

        // Build the filter to get timeline posts
        var filter = Builders<Post>.Filter.Nin(p => p.UserId, bannedUserIds) & Builders<Post>.Filter.Nin(p => p.Id, ignoredPostIds);

        // Add pagination filter if a reference post ID is provided
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _publicPostCollection.Find(p => p.ParentPostId == fromPostId).FirstOrDefaultAsync();
            if (fromPost != null)
            {
                filter &= Builders<Post>.Filter.Lt(p => p.CreatedAt, fromPost.CreatedAt);
            }
        }

        // For reservation posts.
        filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, DateTime.UtcNow);

        // Retrieve and return posts sorted by creation time (newest first)
        return await _publicPostCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetTimelinePostsAsync(string requesterId, string fromPostId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Get IDs of user's friends and add the user's own ID
        var friendIdsResult = await friendshipService.GetFriendIdsAsync(requesterId);
        var relevantUserIds = friendIdsResult.Value;

        var ignoredPostIds = await _ignoredPostCollection
                .Find(i => i.UserId == requesterId)
                .Project(i => i.PostId)
                .ToListAsync();

        // Build the filter to get timeline posts
        var filter = Builders<Post>.Filter.Or(
            // Include all posts created by the user (regardless of privacy settings)
            Builders<Post>.Filter.Eq(p => p.UserId, requesterId),

            // Include posts from friends with appropriate privacy settings
            Builders<Post>.Filter.And(
                Builders<Post>.Filter.In(p => p.UserId, relevantUserIds),
                Builders<Post>.Filter.Or(
                    Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Friends),
                    Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends),
                    Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Everyone)
                )
            ),

            // Include posts where the user is specifically selected as a recipient
            Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.SelectedUsers),
                Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, requesterId)
            ),

            Builders<Post>.Filter.And(
                Builders<Post>.Filter.In(p => p.UserId, relevantUserIds),
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.UnselectedUsers),
                Builders<Post>.Filter.AnyNe(p => p.DiscoveryOptionSelectedUserIds, requesterId)
            )
        );

        // Add pagination filter if a reference post ID is provided
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _postCollection.Find(p => p.Id == fromPostId).FirstOrDefaultAsync();
            if (fromPost != null)
            {
                filter &= Builders<Post>.Filter.Lt(p => p.CreatedAt, fromPost.CreatedAt);
            }
        }

        if(ignoredPostIds.Count > 0)
        {
            filter &= Builders<Post>.Filter.Nin(p => p.Id, ignoredPostIds);
        }

        // For reservation posts.
        filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, DateTime.UtcNow);

        // Retrieve and return posts sorted by creation time (newest first)
        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Result<long>> GetUserPostsCountAsync(string userId, string requesterId = null)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Check relationship between requester and target user
        bool isSelf = requesterId == userId;
        bool areFriends = false;
        bool areFriendsOfFriends = false;

        // Only check relationships if requesterId is provided (logged in user)
        if (!string.IsNullOrEmpty(requesterId) && !isSelf)
        {
            // Check if they are friends
            areFriends = await friendshipService.AreFriendsAsync(requesterId, userId);
            // If not friends, check if they are friends of friends
            if (!areFriends)
            {
                areFriendsOfFriends = await friendshipService.AreFriendsOfFriendsAsync(requesterId, userId);
            }
        }

        // Base filter: posts from the target user
        var filter = Builders<Post>.Filter.Eq(p => p.UserId, userId);
        // Add visibility filters based on privacy settings
        if (!isSelf)
        {
            // Always include public posts
            var visibilityFilter = new List<FilterDefinition<Post>>
            {
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Everyone)
            };

            // Friends or FriendsOfFriends (if friends)
            if (!string.IsNullOrEmpty(requesterId) && areFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    )
                );
            }

            // FriendsOfFriends (if not friends but are FoF)
            if (!string.IsNullOrEmpty(requesterId) && areFriendsOfFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                );
            }

            // SelectedUsers
            if (!string.IsNullOrEmpty(requesterId) && areFriends)
            {
                visibilityFilter.Add(
                    Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                    )
                );

                // UnselectedUsers (NOT in the excluded list)
                visibilityFilter.Add(
                    Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.UnselectedUsers),
                        Builders<Post>.Filter.AnyNe(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                    )
                );
            }

            // Combine all filters
            filter &= Builders<Post>.Filter.Or(visibilityFilter);
        }

        // For reservation posts.
        filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, DateTime.UtcNow);

        // Count the number of posts that match the filter
        return await _postCollection.CountDocumentsAsync(filter);
    }

    /// <inheritdoc/>
    public async Task<Result> IgnorePostAsync(string postId, string userId)
    {
        var post = await GetPostByIdAsync(postId);
        if (post.IsFailure) return post.CastFailure();
        else if (post.Value.UserId == userId) return (ErrorType.BadRequest, "자신의 게시글은 무시할 수 없습니다.");

        var ignoredPost = new IgnoredPost
        {
            UserId = userId,
            PostId = postId
        };
        await _ignoredPostCollection.InsertOneAsync(ignoredPost);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> WritePostAsync(string userId, WritePostRequestDto requestDto, IEnumerable<IFormFile> files)
    {
        if (requestDto.CommentPermission.HasValue)
        {
            if (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers)
                return (ErrorType.BadRequest, "공개 범위가 특정 친구 (비)공개인 게시글은 댓글 작성 권한을 설정할 수 없습니다.");

            var convertedCommentPermission = requestDto.CommentPermission.Value.ToDiscoveryOption();
            if (convertedCommentPermission > requestDto.DiscoveryOption) return (ErrorType.BadRequest, "댓글 작성 권한은 게시글의 공개 범위보다 클 수 없습니다.");
        }

        if (requestDto.ReservationTime.HasValue && requestDto.ReservationTime.Value < DateTime.UtcNow)
            return (ErrorType.BadRequest, "예약 시간이 현재 시간보다 이전일 수 없습니다.");

        var userService = serviceProvider.GetRequiredService<IUserService>();

        var user = await userService.GetUserByIdAsync(userId);
        if (user.IsFailure) return user.CastFailure();

        // Sanitize contents
        var contents = requestDto.Contents ?? [];
        Utils.SanitizeContents(contents);

        // Check if the post has any content
        if (requestDto.ParentPostId == null && (requestDto.Hashtags ?? []).Count == 0 && (contents.Count == 0 || (contents.Count == 1 && contents.First() is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text))))
            return (ErrorType.BadRequest, "게시글에 내용이 없습니다.");

        // Validate media contents
        var mediaCount = contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        var mediaContents = contents.OfType<MediaContent>();
        foreach (var mediaContent in mediaContents)
        {
            if (string.IsNullOrEmpty(mediaContent.MediaId) || string.IsNullOrEmpty(mediaContent.MimeType) || mediaContent.ThumbnailMediaId == null)
            {
                return (ErrorType.BadRequest, "미디어 콘텐츠는 MediaId, MimeType, ThumbnailMediaId가 모두 필요합니다.");
            }
        }

        if ((requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers)
            && (requestDto.DiscoveryOptionSelectedUserIds == null
            || requestDto.DiscoveryOptionSelectedUserIds.Count == 0))
        {
            return (ErrorType.BadRequest, "특정 친구 (비)공개 설정을 선택한 경우, 친구를 선택해야 합니다.");
        }

        if (requestDto.ParentPostId != null)
        {
            var accessResult = await CheckAccessAsync(requestDto.ParentPostId, userId);
            if (accessResult.IsFailure) return accessResult;

            var parentPostResult = await GetPostByIdAsync(requestDto.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure();

            if (parentPostResult.Value.DisallowShare)
                return (ErrorType.BadRequest, "원본 게시글이 공유를 비허용하고 있는 관계로 이 게시글을 공유할 수 없습니다.");

            var parentPost = parentPostResult.Value;
            if (parentPost.DiscoveryOption == DiscoveryOption.SelectedUsers
                || parentPost.DiscoveryOption == DiscoveryOption.UnselectedUsers)
                return (ErrorType.BadRequest, "공개 범위가 특정 친구 (비)공개인 게시글은 공유할 수 없습니다.");
            else if (requestDto.DiscoveryOption > parentPost.DiscoveryOption)
                return (ErrorType.BadRequest, "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다.");
        }

        // Sanitize hashtags and check length
        for (int i = 0; i < requestDto.Hashtags.Count; i++)
        {
            var hashtag = requestDto.Hashtags[i];
            requestDto.Hashtags[i] = Utils.SanitizeText(hashtag);
            hashtag = requestDto.Hashtags[i];
            if (hashtag.Length > 20) return (ErrorType.BadRequest, "해시태그는 최대 20자까지 입력할 수 있습니다.");
        }

        string postId;
        while (true)
        {
            postId = Guid.NewGuid().ToString("N");

            var existingPost = await GetPostByIdAsync(postId);
            if (existingPost.IsFailure) break;
        }

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Post, postId, userId, contents, files);
        if (uploadResult.IsFailure) return uploadResult;

        var externalUrlContents = contents.OfType<ExternalUrlContent>();
        foreach (var externalUrlContent in externalUrlContents.ToList())
        {
            var fillResult = await FillExternalUrlContentAsync(externalUrlContent);
            if (fillResult.IsFailure) contents.Remove(externalUrlContent);
        }

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Contents = contents,
            CreatedAt = requestDto.ReservationTime ?? DateTime.UtcNow,
            DiscoveryOption = requestDto.DiscoveryOption,
            DiscoveryOptionSelectedUserIds = (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers) ? (requestDto.DiscoveryOptionSelectedUserIds ?? []) : null,
            ParentPostId = requestDto.ParentPostId,
            SearchIndex = GenerateSearchIndexFromContents(contents, requestDto.Hashtags),
            CommentPermission = requestDto.CommentPermission,
            DisallowShare = requestDto.DisallowShare,
            Hashtags = requestDto.Hashtags ?? [],
            IsRepost = false
        };

        await _postCollection.InsertOneAsync(post);

        // Update user's last used post discovery option if the post is not a shared post
        if (post.ParentPostId == null)
        {
            var userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var userUpdate = Builders<User>.Update.Set(u => u.LastUsedPostDiscoveryOption, post.DiscoveryOption);
            await _userCollection.UpdateOneAsync(userFilter, userUpdate);
        }

        // Send notification
        if (post.ParentPostId != null) await notificationService.SendNotificationsAsync(NotificationType.Share, post.Id);
        else if (post.Contents.OfType<MediaContent>().Any(x => x.MediaId == "birthday"))
        {
            await notificationService.SendNotificationsAsync(NotificationType.Birthday, post.Id);
        }
        else if (post.Contents.OfType<ProfileContent>().Any()) await notificationService.SendNotificationsAsync(NotificationType.PostMention, post.Id);

        await notificationService.SendNotificationsAsync(NotificationType.FavoriteFriendNewPost, post.Id);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> ModifyPostAsync(string postId, string userId, ModifyPostRequestDto requestDto, IEnumerable<IFormFile> files)
    {
        if (requestDto.CommentPermission.HasValue)
        {
            if (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers)
                return (ErrorType.BadRequest, "공개 범위가 특정 친구 (비)공개인 게시글은 댓글 작성 권한을 설정할 수 없습니다.");

            var convertedCommentPermission = requestDto.CommentPermission.Value.ToDiscoveryOption();
            if (convertedCommentPermission > requestDto.DiscoveryOption) return (ErrorType.BadRequest, "댓글 작성 권한은 게시글의 공개 범위보다 클 수 없습니다.");
        }

        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

        // Sanitize contents
        var contents = requestDto.Contents ?? [];
        Utils.SanitizeContents(contents);

        // Check if the post has any content
        if (postResult.Value.ParentPostId == null && (requestDto.Hashtags ?? []).Count == 0 && (contents.Count == 0 || (contents.Count == 1 && contents.First() is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text))))
            return (ErrorType.BadRequest, "게시글에 내용이 없습니다.");

        if (postResult.Value.ParentPostId != null)
        {
            var parentPostResult = await GetPostByIdAsync(postResult.Value.ParentPostId);
            if (parentPostResult.IsFailure) return parentPostResult.CastFailure();

            var parentPost = parentPostResult.Value;
            if (requestDto.DiscoveryOption > parentPost.DiscoveryOption)
                return (ErrorType.BadRequest, "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다.");
        }

        // Sanitize hashtags and check length
        for (int i = 0; i < requestDto.Hashtags.Count; i++)
        {
            var hashtag = requestDto.Hashtags[i];
            requestDto.Hashtags[i] = Utils.SanitizeText(hashtag);
            hashtag = requestDto.Hashtags[i];
            if (hashtag.Length > 20) return (ErrorType.BadRequest, "해시태그는 최대 20자까지 입력할 수 있습니다.");
        }

        var post = postResult.Value;

        // Check if the user is the author of the post
        if (post.UserId != userId) return (ErrorType.Forbidden, "게시글을 수정할 수 있는 권한이 없습니다.");

        // Validate media contents
        var mediaCount = contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        var originalPostMediaContents = post.Contents.OfType<MediaContent>();
        var mediaContents = contents.OfType<MediaContent>();
        foreach(var mediaContent in mediaContents)
        {
            if (string.IsNullOrEmpty(mediaContent.MediaId) || string.IsNullOrEmpty(mediaContent.MimeType) || mediaContent.ThumbnailMediaId == null)
                return (ErrorType.BadRequest, "미디어 콘텐츠는 MediaId, MimeType, ThumbnailMediaId가 모두 필요합니다.");

            var originalPostMediaContent = originalPostMediaContents.FirstOrDefault(m => m.MediaId == mediaContent.MediaId);
            if (originalPostMediaContent != null)
            {
                if (mediaContent.MimeType != originalPostMediaContent.MimeType)
                    return (ErrorType.BadRequest, "미디어 콘텐츠의 MimeType이 원본과 다릅니다.");
                else if (mediaContent.ThumbnailMediaId != originalPostMediaContent.ThumbnailMediaId)
                    return (ErrorType.BadRequest, "미디어 콘텐츠의 ThumbnailMediaId가 원본과 다릅니다.");
            }
        }

        // Delete Media
        var originalPostMediaIds = originalPostMediaContents.Select(s => s.MediaId).ToList();
        var mediaIds = mediaContents.Select(s => s.MediaId).ToList();

        var deletedMediaIds = originalPostMediaIds.Except(mediaIds).ToList();
        foreach (var mediaId in deletedMediaIds) await mediaService.DeleteMediaByIdAsync(mediaId);

        // Update discovery option (and selected user IDs if applicable)
        post.DiscoveryOption = requestDto.DiscoveryOption;
        post.DiscoveryOptionSelectedUserIds = (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers) ? (requestDto.DiscoveryOptionSelectedUserIds ?? []) : null;

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Post, postId, userId, contents, files);
        if (uploadResult.IsFailure) return uploadResult.CastFailure<Comment>();

        var externalUrlContents = contents.OfType<ExternalUrlContent>();
        foreach (var externalUrlContent in externalUrlContents.ToList())
        {
            var fillResult = await FillExternalUrlContentAsync(externalUrlContent);
            if (fillResult.IsFailure) contents.Remove(externalUrlContent);
        }

        // Update the post contents
        post.Contents = contents;
        post.SearchIndex = GenerateSearchIndexFromContents(contents, requestDto.Hashtags);
        post.CommentPermission = requestDto.CommentPermission;
        post.DisallowShare = requestDto.DisallowShare;
        post.Hashtags = requestDto.Hashtags ?? [];

        post.ModifiedAt = DateTime.UtcNow;

        // Update the post in the database
        await _postCollection.ReplaceOneAsync(p => p.Id == postId, post);

        // Update user's last used post discovery option if the post is not a shared post
        if (post.ParentPostId == null)
        {
            var userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var userUpdate = Builders<User>.Update.Set(u => u.LastUsedPostDiscoveryOption, post.DiscoveryOption);
            await _userCollection.UpdateOneAsync(userFilter, userUpdate);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> DeletePostAsync(string postId, string requesterId)
    {
        var reportService = serviceProvider.GetRequiredService<IReportService>();
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var userResult = await userService.GetUserByIdAsync(requesterId);
        if (userResult.IsFailure) return userResult.CastFailure();

        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();
        var post = postResult.Value;

        // Check permission
        var hasPermission = post.UserId == requesterId || userResult.Value.Rank >= Rank.Moderator;
        if (!hasPermission) return (ErrorType.Forbidden, "게시글을 삭제할 수 있는 권한이 없습니다.");

        // Delete comments and comment likes associated with the post
        var commentIds = await _commentCollection
            .Find(c => c.PostId == postId)
            .Project(c => c.Id)
            .ToListAsync();

        var commentLikeIds = await _commentLikeCollection
            .Find(c => commentIds.Contains(c.CommentId))
            .Project(c => c.Id)
            .ToListAsync();

        await _commentCollection.DeleteManyAsync(c => c.PostId == postId);
        await _commentLikeCollection.DeleteManyAsync(c => commentIds.Contains(c.CommentId));

        // Delete ignored posts associated with the post
        await _ignoredPostCollection.DeleteManyAsync(i => i.PostId == postId);

        // Delete post reactions associated with the post
        await _postReactionCollection.DeleteManyAsync(r => r.PostId == postId);

        // Delete media files associated with the post
        await mediaService.DeleteMediaByAssociatedIdAsync(postId);
        await mediaService.DeleteMediasByAssociatedIdsAsync(commentIds);

        // Delete notifications
        await notificationService.DeleteNotificationsAsync("Data.PostId", postId);

        // Delete reports associated with the post
        await reportService.DeleteReportRecordByPostIdAsync(postId);

        // Delete the post from the database
        await _postCollection.DeleteOneAsync(p => p.Id == postId);
        await _publicPostCollection.DeleteOneAsync(p => p.ParentPostId == postId);

        // Delete reposts of this post from the database
        await _postCollection.DeleteManyAsync(p => p.ParentPostId == postId && p.IsRepost);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> HandleRepostAsync(string postId, string requesterId)
    {
        var accessResult = await CheckAccessAsync(postId, requesterId);
        if (accessResult.IsFailure) return accessResult;

        var existingRepost = await _postCollection
            .Find(p => p.UserId == requesterId && p.ParentPostId == postId && p.IsRepost)
            .FirstOrDefaultAsync();

        var parentPostResult = await GetPostByIdAsync(postId);
        if (parentPostResult.IsFailure) return parentPostResult.CastFailure();

        var parentPost = parentPostResult.Value;

        if (parentPost.DiscoveryOption == DiscoveryOption.SelectedUsers
            || parentPost.DiscoveryOption == DiscoveryOption.UnselectedUsers)
            return (ErrorType.BadRequest, "공개 범위가 특정 친구 (비)공개인 게시글은 리포스트할 수 없습니다.");

        if (existingRepost == null)
        {
            if (parentPostResult.Value.DisallowShare)
                return (ErrorType.BadRequest, "원본 게시글이 공유를 비허용하고 있는 관계로 이 게시글을 공유할 수 없습니다.");

            var post = new Post
            {
                UserId = requesterId,
                DiscoveryOption = DiscoveryOption.Friends,
                ParentPostId = postId,
                IsRepost = true,
                CreatedAt = DateTime.UtcNow,
            };

            while (true)
            {
                post.Id = Guid.NewGuid().ToString("N");

                var existingPost = await GetPostByIdAsync(post.Id);
                if (existingPost.IsFailure) break;
            }

            _postCollection.InsertOne(post);

            // Send notification
            await notificationService.SendNotificationsAsync(NotificationType.Repost, post.Id);
        }
        else
        {
            // If the repost already exists, delete it
            await _postCollection.DeleteOneAsync(p => p.UserId == requesterId && p.ParentPostId == postId && p.IsRepost);

            // Delete notifications
            await notificationService.DeleteNotificationsAsync("AssociatedId", existingRepost.Id, NotificationType.Repost);
        }

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> HandlePostReactionAsync(string postId, string userId, ReactionType type)
    {
        var accessResult = await CheckAccessAsync(postId, userId);
        if (accessResult.IsFailure) return accessResult;

        // Check if the reaction already exists
        var existingReaction = await _postReactionCollection
            .Find(r => r.UserId == userId && r.PostId == postId)
            .FirstOrDefaultAsync();

        if (existingReaction != null)
        {
            await _postReactionCollection.DeleteOneAsync(r => r.UserId == userId && r.PostId == postId);

            // Delete notifications
            await notificationService.DeleteNotificationsAsync("AssociatedId", existingReaction.Id, NotificationType.PostReaction);
        }
        else
        {
            // Add a new reaction
            var newReaction = new PostReaction
            {
                UserId = userId,
                PostId = postId,
                Type = type,
                CreatedAt = DateTime.UtcNow
            };

            while (true)
            {
                newReaction.Id = Guid.NewGuid().ToString("N");
                existingReaction = await _postReactionCollection
                    .Find(r => r.Id == newReaction.Id)
                    .FirstOrDefaultAsync();
                if (existingReaction == null) break;
            }

            await _postReactionCollection.InsertOneAsync(newReaction);

            // Send notification
            await notificationService.SendNotificationsAsync(NotificationType.PostReaction, newReaction.Id);
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> SearchPostsAsync(string query, string requesterId, string fromPostId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var loweredQuery = query.ToLower();
        var filter = Builders<Post>.Filter.Where(p => p.SearchIndex.Contains(loweredQuery));
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _postCollection.Find(p => p.Id == fromPostId).FirstOrDefaultAsync();
            if (fromPost != null)
            {
                filter &= Builders<Post>.Filter.Lt(p => p.CreatedAt, fromPost.CreatedAt);
            }
        }
        if (requesterId != null)
        {
            var requesterBannedFriendIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);
            filter &= Builders<Post>.Filter.Nin(p => p.UserId, requesterBannedFriendIdsResult.Value);
        }

        // For reservation posts.
        filter &= Builders<Post>.Filter.Lte(p => p.CreatedAt, DateTime.UtcNow);

        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> ChangeDiscoveryOptionAsync(string postId, string userId, DiscoveryOption discoveryOption, List<string> selectedUserIds)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

        var post = postResult.Value;

        if (post.ParentPostId != null)
        {
            var parentPostResult = await GetPostByIdAsync(post.ParentPostId);
            if (parentPostResult.IsSuccess && discoveryOption > parentPostResult.Value.DiscoveryOption)
                return (ErrorType.BadRequest, "공유된 글의 공개 범위는 원본 글의 공개 범위보다 클 수 없습니다.");
        }

        // Check if the user is the author of the post
        if (post.UserId != userId) return (ErrorType.Forbidden, "게시글을 수정할 수 있는 권한이 없습니다.");

        // Update discovery option and selected user IDs
        post.DiscoveryOption = discoveryOption;
        post.DiscoveryOptionSelectedUserIds = (discoveryOption == DiscoveryOption.SelectedUsers || discoveryOption == DiscoveryOption.UnselectedUsers) ? selectedUserIds : null;

        var filter = Builders<Post>.Filter.Eq(p => p.Id, postId);
        var update = Builders<Post>.Update
            .Set(p => p.DiscoveryOption, post.DiscoveryOption)
            .Set(p => p.DiscoveryOptionSelectedUserIds, post.DiscoveryOptionSelectedUserIds);

        var result = await _postCollection.UpdateOneAsync(filter, update);
        return result.IsAcknowledged ? Result.Success() : Result.Failure(ErrorType.ProgramError, "게시글의 공개 범위를 변경하는 데 실패했습니다.");
    }

    /// <inheritdoc />
    public async Task<Result> FillExternalUrlContentAsync(ExternalUrlContent externalUrlContent)
    {
        var success = await ExternalUrlHelper.FillExternalUrlContentAsync(externalUrlContent);
        if (!success) return (ErrorType.BadRequest, "URL을 처리하는 데 실패했습니다.");
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> WritePublicPostAsync(string postId, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var originalPostResult = await GetPostByIdAsync(postId);
        if (originalPostResult.IsFailure) return originalPostResult.CastFailure();

        var originalPost = originalPostResult.Value;
        if (originalPost.UserId != requesterId) return (ErrorType.Forbidden, "게시글을 작성한 사용자가 아닙니다.");
        else if (originalPost.IsRepost) return (ErrorType.BadRequest, "리포스트된 게시글은 홍보 게시글로 만들 수 없습니다.");
        else if (originalPost.ParentPostId != null) return (ErrorType.BadRequest, "공유된 게시글은 홍보 게시글로 만들 수 없습니다.");

        var recentPublicPost = await _publicPostCollection
            .Find(p => p.UserId == requesterId)
            .SortByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        var userResult = await userService.GetUserByIdAsync(requesterId);
        if (userResult.IsFailure) return userResult.CastFailure();

        if (userResult.Value.Rank < Rank.Moderator && recentPublicPost != null && (DateTime.UtcNow - recentPublicPost.CreatedAt).TotalDays < 1)
        {
            var remainingTime = TimeSpan.FromDays(1) - (DateTime.UtcNow - recentPublicPost.CreatedAt);
            return (ErrorType.BadRequest, $"홍보 게시글은 24시간마다 한번 씩 작성할 수 있습니다. 남은 시간: {remainingTime.TotalMinutes:N0}분");
        }

        // Sanitize contents
        var contents = originalPost.Contents ?? [];
        Utils.SanitizeContents(contents);

        // Check if the post has any content
        if (contents.Count == 0 || (contents.Count == 1 && contents.First() is TextContent textContent && string.IsNullOrWhiteSpace(textContent.Text)))
            return (ErrorType.BadRequest, "게시글에 내용이 없습니다.");

        var publicPost = new Post
        {
            UserId = requesterId,
            Contents = contents,
            CreatedAt = DateTime.UtcNow,
            DiscoveryOption = DiscoveryOption.Everyone,
            DiscoveryOptionSelectedUserIds = [],
            ParentPostId = postId,
            SearchIndex = originalPost.SearchIndex,
            CommentPermission = AccessPermission.OnlyMe,
            DisallowShare = true,
            Hashtags = originalPost.Hashtags ?? [],
            IsRepost = false,
            IsPublicPost = true
        };

        while (true)
        {
            publicPost.Id = Guid.NewGuid().ToString("N");
            var existingPost = await GetPostByIdAsync(publicPost.Id);
            if (existingPost.IsFailure) break;
        }

        await _publicPostCollection.DeleteManyAsync(x => x.UserId == requesterId);
        await _publicPostCollection.InsertOneAsync(publicPost);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CheckCommentAccessAsync(string postId, string requesterId)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

        return await CheckCommentAccessAsync(postResult, requesterId);
    }

    /// <inheritdoc />
    public async Task<Result> CheckCommentAccessAsync(Post post, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var requesterResult = await userService.GetUserByIdAsync(requesterId);
        if (requesterResult.IsFailure) return requesterResult.CastFailure();

        if (requesterResult.Value.Rank >= Rank.Moderator) return Result.Success(); // Moderators can access all posts

        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();


        var postAuthorId = post.UserId;
        if (postAuthorId == requesterId) return Result.Success();

        // Apply discovery option / privacy settings
        var commentPermission = post.CommentPermission?.ToDiscoveryOption() ?? post.DiscoveryOption;
        if (commentPermission < DiscoveryOption.Everyone)
        {
            bool hasAccess;
            if (commentPermission == DiscoveryOption.FriendsOfFriends) hasAccess = await friendshipService.AreFriendsOfFriendsAsync(postAuthorId, requesterId);
            else if (commentPermission == DiscoveryOption.Friends) hasAccess = await friendshipService.AreFriendsAsync(postAuthorId, requesterId);
            else if (commentPermission == DiscoveryOption.SelectedUsers) hasAccess = post.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (commentPermission == DiscoveryOption.UnselectedUsers) hasAccess = !post.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (commentPermission == DiscoveryOption.OnlyMe) hasAccess = postAuthorId == requesterId;
            else
            {
                var requesterBlockerIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);
                if (requesterBlockerIdsResult.IsFailure) return requesterBlockerIdsResult;
                else if (requesterBlockerIdsResult.Value.Contains(postAuthorId)) hasAccess = false;
                else hasAccess = true;
            }

            if (!hasAccess)
            {
                if (post.CommentPermission.HasValue) return (ErrorType.Forbidden, $"이 게시물에 대한 댓글 작성 권한이 없습니다. 설정된 권한: {post.CommentPermission.Value.ToDisplayString()}");
                else return (ErrorType.Forbidden, "이 게시물에 대한 댓글 작성 권한이 없습니다.");
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CheckAccessAsync(string postId, string requesterId)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

        return await CheckAccessAsync(postResult, requesterId);
    }

    public async Task<Result> CheckAccessAsync(Post post, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var requesterResult = await userService.GetUserByIdAsync(requesterId);
        if (requesterResult.IsFailure) return requesterResult.CastFailure();

        if (requesterResult.Value.Rank >= Rank.Moderator) return Result.Success(); // Moderators can access all posts

        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();


        var postAuthorId = post.UserId;
        if (postAuthorId == requesterId) return Result.Success();

        // Apply discovery option / privacy settings
        var postDiscoveryOption = post.DiscoveryOption;
        if (postDiscoveryOption < DiscoveryOption.Everyone)
        {
            bool hasAccess;
            if (postDiscoveryOption == DiscoveryOption.FriendsOfFriends) hasAccess = await friendshipService.AreFriendsOfFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == DiscoveryOption.Friends) hasAccess = await friendshipService.AreFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == DiscoveryOption.SelectedUsers) hasAccess = post.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (postDiscoveryOption == DiscoveryOption.UnselectedUsers) hasAccess = !post.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (postDiscoveryOption == DiscoveryOption.OnlyMe) hasAccess = postAuthorId == requesterId;
            else
            {
                var requesterBlockerIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);
                if (requesterBlockerIdsResult.IsFailure) return requesterBlockerIdsResult;
                else if (requesterBlockerIdsResult.Value.Contains(postAuthorId)) hasAccess = false;
                else hasAccess = true;
            }

            if (!hasAccess) return (ErrorType.Forbidden, "이 게시물에 대한 접근 권한이 없습니다.");
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result<PostResponseDto>> GeneratePostResponseDtoAsync(Post post, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();
        var commentService = serviceProvider.GetRequiredService<ICommentService>();

        var userResult = await userService.GenerateUserResponseDtoAsync(post.UserId, requesterId);
        if (userResult.IsFailure) return userResult.CastFailure<Result<PostResponseDto>>();

        var profileContents = post.Contents.OfType<ProfileContent>();
        var profileContentUsersResult = await userService.GenerateUserResponseDtosAsync(profileContents.Select(x => x.UserId), requesterId);
        foreach (var profileContent in profileContents)
        {
            var user = profileContentUsersResult.Value.FirstOrDefault(x => x.UserId == profileContent.UserId);
            profileContent.UserId = user?.UserId;
            profileContent.Nickname = (user?.Nickname ?? "탈퇴한 사용자") + ' ';
        }

        if (post.IsPublicPost)
        {
            return new PostResponseDto
            {
                Id = post.ParentPostId,
                User = userResult.Value,
                DiscoveryOption = DiscoveryOption.Everyone,
                DiscoveryOptionSelectedUserIds = [],
                Contents = post.Contents,
                Comments = [],
                CommentsCount = 0,
                PostReactions = [],
                SharedAndRepostedUsers = [],
                CommentPermission = post.CommentPermission,
                DisallowShare = post.DisallowShare,
                IsRepost = false,
                ModifiedAt = null,
                CreatedAt = post.CreatedAt
            };
        }

        var commentsResult = await commentService.GetCommentsByPostIdAsync(post.Id, requesterId, null, 20);
        if (commentsResult.IsFailure) return commentsResult.CastFailure<Result<PostResponseDto>>();
        var commentDtosResult = await commentService.GenerateCommentResponseDtosAsync(commentsResult.Value, requesterId);

        var commentsCountResult = await commentService.GetCommentsCountByPostIdAsync(post.Id, requesterId);
        if (commentsCountResult.IsFailure) return commentsCountResult.CastFailure<Result<PostResponseDto>>();

        var postReactionDtos = await GeneratePostReactionDtosAsync(post.Id, requesterId);
        var sharedAndRepostedUserDtos = await GenerateSharedAndRepostedUserDtosAsync(post.Id, requesterId);

        var postResponse = new PostResponseDto
        {
            Id = post.Id,
            User = userResult.Value,
            DiscoveryOption = post.DiscoveryOption,
            DiscoveryOptionSelectedUserIds = post.DiscoveryOptionSelectedUserIds,
            Contents = post.Contents,
            Comments = commentDtosResult.Value,
            CommentsCount = commentsCountResult.Value,
            PostReactions = postReactionDtos.Value,
            SharedAndRepostedUsers = sharedAndRepostedUserDtos.Value,
            CommentPermission = post.CommentPermission,
            DisallowShare = post.DisallowShare,
            Hashtags = post.Hashtags ?? [],
            IsRepost = post.IsRepost,
            CreatedAt = post.CreatedAt,
            ModifiedAt = post.ModifiedAt
        };

        if (post.ParentPostId != null)
        {
            var parentPostResult = await GetPostByIdAsync(post.ParentPostId);
            if (parentPostResult.IsFailure) return postResponse;

            var requesterBannedFriendIdsResult = await friendshipService.GetBannedUserIdsAsync(requesterId);
            if (requesterBannedFriendIdsResult.IsFailure) return requesterBannedFriendIdsResult.CastFailure<PostResponseDto>();

            var isBanned = requesterBannedFriendIdsResult.Value.Contains(parentPostResult.Value.UserId);

            var parentPostUserResult = await userService.GenerateUserResponseDtoAsync(parentPostResult.Value.UserId, requesterId);
            var parentPostSharedAndRepostedUserDtos = await GenerateSharedAndRepostedUserDtosAsync(post.ParentPostId, requesterId);

            var parentPostProfileContents = parentPostResult.Value.Contents.OfType<ProfileContent>();
            var parentPostProfileContentUsersResult = await userService.GenerateUserResponseDtosAsync(parentPostProfileContents.Select(x => x.UserId), requesterId);
            foreach (var parentPostProfileContent in parentPostProfileContents)
            {
                var user = parentPostProfileContentUsersResult.Value.FirstOrDefault(x => x.UserId == parentPostProfileContent.UserId);
                parentPostProfileContent.UserId = user?.UserId;
                parentPostProfileContent.Nickname = (user?.Nickname ?? "탈퇴한 사용자") + ' ';
            }

            if (!isBanned && parentPostUserResult.IsSuccess)
            {
                postResponse.ParentPost = new PostResponseDto
                {
                    Id = parentPostResult.Value.Id,
                    User = parentPostUserResult.Value,
                    DiscoveryOption = parentPostResult.Value.DiscoveryOption,
                    DiscoveryOptionSelectedUserIds = parentPostResult.Value.DiscoveryOptionSelectedUserIds,
                    Contents = parentPostResult.Value.Contents,
                    SharedAndRepostedUsers = parentPostSharedAndRepostedUserDtos,
                    CommentPermission = parentPostResult.Value.CommentPermission,
                    DisallowShare = parentPostResult.Value.DisallowShare,
                    Hashtags = parentPostResult.Value.Hashtags ?? [],
                    IsRepost = parentPostResult.Value.IsRepost,
                    CreatedAt = parentPostResult.Value.CreatedAt,
                    ModifiedAt = parentPostResult.Value.ModifiedAt
                };
            }
        }

        return postResponse;
    }

    /// <inheritdoc />
    public async Task<Result<List<SharedAndRepostedUserDto>>> GenerateSharedAndRepostedUserDtosAsync(string postId, string requesterId)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var posts = await _postCollection
            .Find(p => p.ParentPostId == postId)
            .Project(p => new { p.UserId, p.Id, p.IsRepost, p.CreatedAt })
            .ToListAsync();
        var sharedUserIds = posts.Select(x => x.UserId);
        var sharedUsersResult = await userService.GenerateUserResponseDtosAsync(sharedUserIds, requesterId);

        var sharedUserDtos = new List<SharedAndRepostedUserDto>();
        foreach (var post in posts)
        {
            var sharedUser = sharedUsersResult.Value.FirstOrDefault(x => x.UserId == post.UserId);
            if (sharedUser == null) continue;

            var sharedUserDto = new SharedAndRepostedUserDto
            {
                User = sharedUser,
                PostId = post.Id,
                IsRepost = post.IsRepost,
                SharedAt = post.CreatedAt
            };
            sharedUserDtos.Add(sharedUserDto);
        }

        return sharedUserDtos;
    }

    /// <inheritdoc/>
    public async Task<Result<List<PostResponseDto>>> GeneratePostResponseDtosAsync(List<Post> posts, string requesterId)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var bannedUserIds = new List<string>();
        if (requesterId != null) bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);

        var tasks = posts.Select(x => GeneratePostResponseDtoAsync(x, requesterId)).ToList();
        await Task.WhenAll(tasks);

        return tasks.Where(x => x.Result.IsSuccess).Select(x => x.Result.Value).ToList();
    }

    /// <inheritdoc/>
    public async Task<Result> HandleWithdrawAsync(string userId)
    {
        var notificationService = serviceProvider.GetRequiredService<INotificationService>();

        if (userId == null) return (ErrorType.BadRequest, "유저 ID가 제공되지 않았습니다.");

        var postIds = await _postCollection
            .Find(p => p.UserId == userId)
            .Project(p => p.Id)
            .ToListAsync();

        if (postIds.Count == 0) return Result.Success();

        // Delete comments and comment likes associated with the posts
        var commentIds = await _commentCollection
            .Find(c => postIds.Contains(c.PostId))
            .Project(c => c.Id)
            .ToListAsync();

        await _commentCollection.DeleteManyAsync(c => postIds.Contains(c.PostId));
        await _commentLikeCollection.DeleteManyAsync(c => commentIds.Contains(c.CommentId));

        // Delete ignored posts associated with the posts
        await _ignoredPostCollection.DeleteManyAsync(i => postIds.Contains(i.PostId));

        // Delete post reactions associated with the posts
        await _postReactionCollection.DeleteManyAsync(r => postIds.Contains(r.PostId));

        // Delete notifications associated with the posts
        await notificationService.DeleteNotificationsAsync("Data.PostId", postIds);

        // Delete notifications associated with the comments
        await notificationService.DeleteNotificationsAsync("Data.CommentId", commentIds);

        // Delete media files associated with the posts
        await mediaService.DeleteMediasByAssociatedIdsAsync(postIds);

        // Delete media files associated with the comments
        await mediaService.DeleteMediasByAssociatedIdsAsync(commentIds);

        // Delete posts
        await _postCollection.DeleteManyAsync(p => p.UserId == userId);
        await _publicPostCollection.DeleteManyAsync(p => p.UserId == userId);

        return Result.Success();
    }

    private static string GenerateSearchIndexFromContents(IEnumerable<BaseContent> contents, IEnumerable<string> hashtags)
    {
        var body = string.Join(" ", contents.OfType<TextContent>().Select(s => s.Text))
        .ReplaceLineEndings()
        .ToLower()
        .Replace(Environment.NewLine, " ");

        hashtags = hashtags.Select(x => $"#{x}").ToList();
        var hashtag = string.Join(" ", hashtags ?? [])
            .ToLower();

        return $"{body} {hashtag}".Trim();
    }

    private async Task<Result<List<PostReactionDto>>> GeneratePostReactionDtosAsync(string postId, string requesterId = null)
    {
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var postReactions = await _postReactionCollection
            .Find(r => r.PostId == postId)
            .ToListAsync();

        var userIds = postReactions.Select(r => r.UserId).Distinct().ToList();
        var userResponseDtos = await userService.GenerateUserResponseDtosAsync(userIds, requesterId);

        var results = new List<PostReactionDto>();
        foreach(var postReaction in postReactions)
        {
            var user = userResponseDtos.Value.FirstOrDefault(x => x.UserId == postReaction.UserId);
            if (user != null)
            {
                var result = new PostReactionDto
                {
                    User = user,
                    Type = postReaction.Type,
                    CreatedAt = postReaction.CreatedAt
                };
                results.Add(result);
            }
        }

        return results;
    }
}
