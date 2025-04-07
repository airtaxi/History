using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using MongoDB.Driver;
using System.Collections.Generic;

namespace History.ApiService.Services;

public class PostService(IMongoDatabase database, IFriendshipService friendshipService) : IPostService
{
    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");
    private readonly IMongoCollection<IgnoredPost> _ignoredPostCollection = database.GetCollection<IgnoredPost>("IgnoredPosts");

    /// <inheritdoc />
    public async Task<Result<Post>> GetPostByIdAsync(string postId) => await _postCollection.Find(p => p.Id == postId).FirstOrDefaultAsync();

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
}
