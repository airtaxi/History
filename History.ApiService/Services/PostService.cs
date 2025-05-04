using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Bson;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class PostService(IMongoDatabase database, IMediaService mediaService, IServiceProvider serviceProvider) : IPostService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");
    private readonly IMongoCollection<Comment> _commentCollection = database.GetCollection<Comment>("Comments");
    private readonly IMongoCollection<CommentLike> _commentLikeCollection = database.GetCollection<CommentLike>("CommentLikes");

    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<IgnoredPost> _ignoredPostCollection = database.GetCollection<IgnoredPost>("IgnoredPosts");
    private readonly IMongoCollection<PostReaction> _postReactionCollection = database.GetCollection<PostReaction>("PostReactions");

    /// <inheritdoc />
    public async Task<Result<Post>> GetPostByIdAsync(string postId)
    {
        var post = await _postCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();

        if (post == null) return (ErrorType.NotFound, "게시글을 찾을 수 없습니다.");
        else return post;
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetUserPostsAsync(string requesterId, string userId, string fromPostId = null, int limit = 10)
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
            if (!string.IsNullOrEmpty(requesterId))
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

        // Retrieve and return posts sorted by creation time (newest first)
        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetTimelinePostsAsync(string userId, string fromPostId = null, int limit = 10)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        // Get IDs of user's friends and add the user's own ID
        var friendIdsResult = await friendshipService.GetFriendIdsAsync(userId);
        var relevantUserIds = friendIdsResult.Value;

        var ignoredPostIds = await _ignoredPostCollection
                .Find(i => i.UserId == userId)
                .Project(i => i.PostId)
                .ToListAsync();

        // Build the filter to get timeline posts
        var filter = Builders<Post>.Filter.Or(
            // Include all posts created by the user (regardless of privacy settings)
            Builders<Post>.Filter.Eq(p => p.UserId, userId),

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
                Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, userId)
            ),

            Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.UnselectedUsers),
                Builders<Post>.Filter.AnyNe(p => p.DiscoveryOptionSelectedUserIds, userId)
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
            if (!string.IsNullOrEmpty(requesterId))
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

        // Count the number of posts that match the filter
        return await _postCollection.CountDocumentsAsync(filter);
    }

    /// <inheritdoc/>
    public async Task<Result> IgnorePostAsync(string userId, string postId)
    {
        var post = await GetPostByIdAsync(postId);
        if (post.IsFailure) return post.CastFailure();
        else if (post.Value.UserId == userId) return Result.Failure(ErrorType.BadRequest, "자신의 게시글은 무시할 수 없습니다.");

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
        var userService = serviceProvider.GetRequiredService<IUserService>();

        var user = await userService.GetUserByIdAsync(userId);
        if (user.IsFailure) return user.CastFailure();

        var mediaCount = requestDto.Contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        if ((requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers)
            && (requestDto.DiscoveryOptionSelectedUserIds == null
            || requestDto.DiscoveryOptionSelectedUserIds.Count == 0))
        {
            return Result.Failure(ErrorType.BadRequest, "특정 친구 (비)공개 설정을 선택한 경우, 친구를 선택해야 합니다.");
        }

        if (requestDto.ParentPostId != null)
        {
            var accessResult = await CheckAccessAsync(requestDto.ParentPostId, userId);
            if (accessResult.IsFailure) return accessResult;
        }

        string postId;
        while (true)
        {
            postId = Guid.NewGuid().ToString("N");

            var existingPost = await GetPostByIdAsync(postId);
            if (existingPost.IsFailure) break;
        }

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Post, postId, userId, requestDto.Contents, files);
        if (uploadResult.IsFailure) return uploadResult;

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Contents = requestDto.Contents,
            CreatedAt = DateTime.UtcNow,
            DiscoveryOption = requestDto.DiscoveryOption,
            DiscoveryOptionSelectedUserIds = (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers) ? requestDto.DiscoveryOptionSelectedUserIds : null,
            ParentPostId = requestDto.ParentPostId,
            SearchIndex = GenerateSearchIndexFromContents(requestDto.Contents),
            IsRepost = false
        };

        await _postCollection.InsertOneAsync(post);

        var userFilter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var userUpdate = Builders<User>.Update.Set(u => u.LastUsedPostDiscoveryOption, post.DiscoveryOption);
        await _userCollection.UpdateOneAsync(userFilter, userUpdate);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> ModifyPostAsync(string postId, string userId, ModifyPostRequestDto requestDto, IEnumerable<IFormFile> files)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

        var mediaCount = requestDto.Contents.Count(x => x is UploadContent || x is MediaContent);
        if (mediaCount > 20) return (ErrorType.BadRequest, "미디어는 최대 20개까지 추가할 수 있습니다.");

        var post = postResult.Value;
        // Check if the user is the author of the post
        if (post.UserId != userId) return Result.Failure(ErrorType.Forbidden, "게시글을 수정할 수 있는 권한이 없습니다.");

        // Delete Media
        var originalPostMediaIds = post.Contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();
        var mediaIds = requestDto.Contents.OfType<MediaContent>().Select(s => s.MediaId).ToList();

        var deletedMediaIds = originalPostMediaIds.Except(mediaIds).ToList();
        foreach (var mediaId in deletedMediaIds) await mediaService.DeleteMediaByIdAsync(mediaId);

        // Update discovery option (and selected user IDs if applicable)
        post.DiscoveryOption = requestDto.DiscoveryOption;
        post.DiscoveryOptionSelectedUserIds = (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers || requestDto.DiscoveryOption == DiscoveryOption.UnselectedUsers) ? requestDto.DiscoveryOptionSelectedUserIds : null;

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Post, postId, userId, requestDto.Contents, files);
        if (uploadResult.IsFailure) return uploadResult.CastFailure<Comment>();

        // Update the post contents
        post.Contents = requestDto.Contents;
        post.SearchIndex = GenerateSearchIndexFromContents(requestDto.Contents);

        // Update the post in the database
        await _postCollection.ReplaceOneAsync(p => p.Id == postId, post);
        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> DeletePostAsync(string userId, string postId)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();
        var post = postResult.Value;

        // Check if the user is the author of the post
        if (post.UserId != userId) return Result.Failure(ErrorType.Forbidden, "게시글을 삭제할 수 있는 권한이 없습니다.");

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

        // Delete the post from the database
        await _postCollection.DeleteOneAsync(p => p.Id == postId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> RepostPostAsync(string userId, string postId)
    {
        var accessResult = await CheckAccessAsync(postId, userId);
        if (accessResult.IsFailure) return accessResult;

        var post = new Post
        {
            UserId = userId,
            DiscoveryOption = DiscoveryOption.Friends,
            ParentPostId = postId,
            IsRepost = true,
        };

        while (true)
        {
            post.Id = Guid.NewGuid().ToString("N");

            var existingPost = await GetPostByIdAsync(post.Id);
            if (existingPost.IsFailure) break;
        }

        _postCollection.InsertOne(post);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> HandlePostReactionAsync(string userId, string postId, PostReactionType type)
    {
        var accessResult = await CheckAccessAsync(postId, userId);
        if (accessResult.IsFailure) return accessResult;

        // Check if the reaction already exists
        var existingReaction = await _postReactionCollection
            .Find(r => r.UserId == userId && r.PostId == postId)
            .FirstOrDefaultAsync();

        if (existingReaction != null) await _postReactionCollection.DeleteOneAsync(r => r.UserId == userId && r.PostId == postId);
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

        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(p => p.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<Result> CheckAccessAsync(string postId, string requesterId)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) postResult.CastFailure();

        return await CheckAccessAsync(postResult, requesterId);
    }

    public async Task<Result> CheckAccessAsync(Post post, string requesterId)
    {
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

        var commentsResult = await commentService.GetCommentsByPostIdAsync(post.Id, requesterId, null, 20);
        if (commentsResult.IsFailure) return commentsResult.CastFailure<Result<PostResponseDto>>();
        var commentDtosResult = await commentService.GenerateCommentResponseDtosAsync(commentsResult.Value, requesterId);

        var commentsCountResult = await commentService.GetCommentsCountByPostIdAsync(post.Id, requesterId);
        if (commentsCountResult.IsFailure) return commentsCountResult.CastFailure<Result<PostResponseDto>>();

        var profileContents = post.Contents.OfType<ProfileContent>();
        var profileContentUsersResult = await userService.GenerateUserResponseDtosAsync(profileContents.Select(x => x.UserId), requesterId);
        foreach (var profileContent in profileContents)
        {
            var user = profileContentUsersResult.Value.FirstOrDefault(x => x.UserId == profileContent.UserId);
            profileContent.UserId = user?.UserId;
            profileContent.Nickname = (user?.Nickname ?? "차단된 사용자") + ' ';
        }

        var postReactions = await GeneratePostReactionDtosAsync(post.Id, requesterId);

        var hasBeenSimpleReposted = requesterId != null ? await _postCollection
                .Find(p => p.ParentPostId == post.Id && p.UserId == requesterId && p.Contents == null)
                .AnyAsync() : false;

        var postResponse = new PostResponseDto
        {
            Id = post.Id,
            User = userResult.Value,
            Contents = post.Contents,
            Comments = commentDtosResult.Value,
            CommentsCount = commentsCountResult.Value,
            PostReactions = postReactions,
            IsRepost = post.IsRepost,
            HasBeenSimpleReposted = hasBeenSimpleReposted,
            CreatedAt = post.CreatedAt,
            ModifiedAt = post.ModifiedAt
        };

        if (post.ParentPostId != null)
        {
            var parentPostResult = await GetPostByIdAsync(post.ParentPostId);
            var hasAccessResult = await CheckAccessAsync(parentPostResult.Value, requesterId);
            var parentPostUserResult = await userService.GenerateUserResponseDtoAsync(parentPostResult.Value.UserId, requesterId);
            if (parentPostResult.IsSuccess && hasAccessResult.IsSuccess && parentPostUserResult.IsSuccess)
            {
                postResponse.ParentPost = new PostResponseDto
                {
                    Id = parentPostResult.Value.Id,
                    User = parentPostUserResult.Value,
                    Contents = parentPostResult.Value.Contents,
                    IsRepost = parentPostResult.Value.IsRepost,
                    CreatedAt = parentPostResult.Value.CreatedAt,
                    ModifiedAt = parentPostResult.Value.ModifiedAt
                };
            }
        }

        return postResponse;
    }

    /// <inheritdoc />
    public async Task<Result<List<PostResponseDto>>> GeneratePostResponseDtosAsync(List<Post> posts, string requesterId)
    {
        var friendshipService = serviceProvider.GetRequiredService<IFriendshipService>();

        var bannedUserIds = new List<string>();
        if (requesterId != null) bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);

        var tasks = posts.Select(x => GeneratePostResponseDtoAsync(x, requesterId)).ToList();
        await Task.WhenAll(tasks);

        return tasks.Where(x => x.Result.IsSuccess).Select(x => x.Result.Value).ToList();
    }

    private static string GenerateSearchIndexFromContents(IEnumerable<BaseContent> contents) =>
        string.Join(" ", contents.OfType<TextContent>().Select(s => s.Text))
        .ReplaceLineEndings()
        .ToLower()
        .Replace(Environment.NewLine, " ");

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
