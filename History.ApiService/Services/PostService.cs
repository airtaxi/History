using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using Microsoft.VisualBasic;
using MongoDB.Driver;
using System.Collections.Generic;

namespace History.ApiService.Services;

public class PostService(IMongoDatabase database, IFriendshipService friendshipService, IUserService userService, IMediaService mediaService) : IPostService
{
    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<IgnoredPost> _ignoredPostCollection = database.GetCollection<IgnoredPost>("IgnoredPosts");

    /// <inheritdoc />
    public async Task<Result<Post>> GetPostByIdAsync(string postId)
    {
        var post = await _postCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();

        if (post == null) return Result<Post>.Failure(ErrorType.NotFound, "게시글을 찾을 수 없습니다.");
        else return post;
    }

    /// <inheritdoc />
    public async Task<Result<List<Post>>> GetUserPostsAsync(string requesterId, string userId, string fromPostId = null, int limit = 10)
    {
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
            filter &= Builders<Post>.Filter.Or(
                // Public posts are always visible (even to non-logged in users)
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Everyone),

                // For logged in users who are friends, include posts with Friends or higher visibility
                !string.IsNullOrEmpty(requesterId) && areFriends
                    ? Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                      )
                    : Builders<Post>.Filter.Empty,

                // For logged in users who are friends of friends, include posts with FriendsOfFriends visibility
                !string.IsNullOrEmpty(requesterId) && areFriendsOfFriends
                    ? Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    : Builders<Post>.Filter.Empty,

                // For logged in users included in SelectedUsers
                !string.IsNullOrEmpty(requesterId)
                    ? Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                      )
                    : Builders<Post>.Filter.Empty
            );

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
            var visibilityFilter = Builders<Post>.Filter.Or(
                // Public posts are always visible (even to non-logged in users)
                Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Everyone),
                // For logged in users who are friends, include posts with Friends or higher visibility
                !string.IsNullOrEmpty(requesterId) && areFriends
                    ? Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                      )
                    : Builders<Post>.Filter.Empty,
                // For logged in users who are friends of friends, include posts with FriendsOfFriends visibility
                !string.IsNullOrEmpty(requesterId) && areFriendsOfFriends
                    ? Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    : Builders<Post>.Filter.Empty,
                // For logged in users included in SelectedUsers
                !string.IsNullOrEmpty(requesterId)
                    ? Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(p => p.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(p => p.DiscoveryOptionSelectedUserIds, requesterId)
                      )
                    : Builders<Post>.Filter.Empty
            );
            filter = Builders<Post>.Filter.And(filter, visibilityFilter);
        }

        // Count the number of posts that match the filter
        return await _postCollection.CountDocumentsAsync(filter);
    }

    /// <inheritdoc/>
    public async Task<Result> IgnorePostAsync(string userId, string postId)
    {
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
        var user = await userService.GetUserByIdAsync(userId);
        if (user.IsFailure) return user.CastFailure();

        if (requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers
            && (requestDto.DiscoveryOptionSelectedUserIds == null
            || requestDto.DiscoveryOptionSelectedUserIds.Count == 0))
        {
            return Result.Failure(ErrorType.BadRequest, "편한 친구 공개 설정을 선택한 경우, 친구를 선택해야 합니다.");
        }

        if (requestDto.ParentPostId != null)
        {
            var parentPost = await GetPostByIdAsync(requestDto.ParentPostId);
            if (parentPost.IsFailure) return parentPost.CastFailure();

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
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, postId, userId, requestDto.Contents, files);
        if (uploadResult.IsFailure) return uploadResult;

        var post = new Post
        {
            Id = postId,
            UserId = userId,
            Contents = requestDto.Contents,
            CreatedAt = DateTime.UtcNow,
            DiscoveryOption = requestDto.DiscoveryOption,
            DiscoveryOptionSelectedUserIds = requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers ? requestDto.DiscoveryOptionSelectedUserIds : null,
            ParentPostId = requestDto.ParentPostId,
            IsRepost = false
        };

        await _postCollection.InsertOneAsync(post);

        return Result.Success();
    }

    /// <inheritdoc/>
    public async Task<Result> ModifyPostAsync(string userId, string postId, ModifyPostRequestDto requestDto, IEnumerable<IFormFile> files)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return postResult.CastFailure();

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
        post.DiscoveryOptionSelectedUserIds = requestDto.DiscoveryOption == DiscoveryOption.SelectedUsers ? requestDto.DiscoveryOptionSelectedUserIds : null;

        // Upload medias
        var uploadResult = await mediaService.HandleUploadContentsAsync(MediaBucket.Comment, postId, userId, requestDto.Contents, files);
        if (uploadResult.IsFailure) return Result<Comment>.Failure(uploadResult);

        // Update the post contents
        post.Contents = requestDto.Contents;

        // Update the post in the database
        await _postCollection.ReplaceOneAsync(p => p.Id == postId, post);
        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> CheckAccessAsync(string postId, string requesterId)
    {
        var postResult = await GetPostByIdAsync(postId);
        if (postResult.IsFailure) return Result<Comment>.Failure(ErrorType.NotFound, "게시글을 찾을 수 없습니다.");

        var postAuthorId = postResult.Value.UserId;

        // Apply discovery option / privacy settings
        var postDiscoveryOption = postResult.Value.DiscoveryOption;
        if (postDiscoveryOption < DiscoveryOption.Everyone)
        {
            bool hasAccess;
            if (postDiscoveryOption == DiscoveryOption.FriendsOfFriends) hasAccess = await friendshipService.AreFriendsOfFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == DiscoveryOption.Friends) hasAccess = await friendshipService.AreFriendsAsync(postAuthorId, requesterId);
            else if (postDiscoveryOption == DiscoveryOption.SelectedUsers) hasAccess = postResult.Value.DiscoveryOptionSelectedUserIds.Contains(requesterId);
            else if (postDiscoveryOption == DiscoveryOption.OnlyMe) hasAccess = postAuthorId == requesterId;
            else
            {
                var requesterBlockerIdsResult = await friendshipService.GetBlockerUserIdsAsync(requesterId);
                if (requesterBlockerIdsResult.IsFailure) return requesterBlockerIdsResult;
                else if (requesterBlockerIdsResult.Value.Contains(postAuthorId)) hasAccess = false;
                else hasAccess = true;
            }

            if (!hasAccess) return Result<Comment>.Failure(ErrorType.Forbidden, "이 게시물에 대한 접근 권한이 없습니다.");
        }

        return Result.Success();
    }

    public async Task<Result<PostResponseDto>> GeneratePostResponseDtoAsync(Post post, IEnumerable<string> bannedUserIds)
    {
        if (bannedUserIds.Contains(post.UserId)) return null;

        var postResponse = new PostResponseDto
        {
            Id = post.Id,
            UserId = post.UserId,
            Contents = post.Contents,
            CreatedAt = post.CreatedAt,
            IsRepost = post.IsRepost,
            ModifiedAt = post.ModifiedAt
        };

        if (post.ParentPostId != null)
        {
            var parentPostResult = await GetPostByIdAsync(post.ParentPostId);
            if (parentPostResult.Error != null && !bannedUserIds.Contains(parentPostResult.Value.UserId))
            {
                postResponse.ParentPost = new PostResponseDto
                {
                    Id = parentPostResult.Value.Id,
                    UserId = parentPostResult.Value.UserId,
                    Contents = parentPostResult.Value.Contents,
                    CreatedAt = parentPostResult.Value.CreatedAt,
                    IsRepost = parentPostResult.Value.IsRepost,
                    ModifiedAt = parentPostResult.Value.ModifiedAt
                };
            }
        }

        return postResponse;
    }

    public async Task<Result<List<PostResponseDto>>> GeneratePostResponsesDtosAsync(List<Post> posts, string requesterId)
    {
        var bannedUserIds = new List<string>();
        if (requesterId != null) bannedUserIds = await friendshipService.GetBannedUserIdsAsync(requesterId);

        var tasks = posts.Select(x => GeneratePostResponseDtoAsync(x, bannedUserIds)).ToList();
        await Task.WhenAll(tasks);

        return tasks.Where(x => x.Result.IsSuccess).Select(x => x.Result.Value).ToList();
    }
}
