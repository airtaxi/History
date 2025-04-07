using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;
using System.Collections.Generic;

namespace History.ApiService.Services;

public class PostService(IMongoDatabase database, IFriendshipService friendshipService) : IPostService
{
    private readonly IMongoCollection<Post> _postCollection = database.GetCollection<Post>("Posts");

    public async Task<Result<Post>> GetPostByIdAsync(string postId) => await _postCollection.Find(f => f.Id == postId).FirstOrDefaultAsync();

    public async Task<Result<List<Post>>> GetUserPostsAsync(string requesterId, string userId, string fromPostId = null, int limit = 10)
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
        var filter = Builders<Post>.Filter.Eq(f => f.UserId, userId);

        // Add visibility filters based on privacy settings
        if (!isSelf)
        {
            var visibilityFilter = Builders<Post>.Filter.Or(
                // Public posts are always visible (even to non-logged in users)
                Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Everyone),

                // For logged in users who are friends, include posts with Friends or higher visibility
                !string.IsNullOrEmpty(requesterId) && areFriends
                    ? Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                      )
                    : Builders<Post>.Filter.Empty,

                // For logged in users who are friends of friends, include posts with FriendsOfFriends visibility
                !string.IsNullOrEmpty(requesterId) && areFriendsOfFriends
                    ? Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    : Builders<Post>.Filter.Empty,

                // For logged in users included in SelectedUsers
                !string.IsNullOrEmpty(requesterId)
                    ? Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(f => f.DiscoveryOptionSelectedUserIds, requesterId)
                      )
                    : Builders<Post>.Filter.Empty
            );

            filter = Builders<Post>.Filter.And(filter, visibilityFilter);
        }

        // Add pagination filter
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _postCollection.Find(f => f.Id == fromPostId).FirstOrDefaultAsync();
            if (fromPost != null)
            {
                var timeFilter = Builders<Post>.Filter.Lt(f => f.CreatedAt, fromPost.CreatedAt);
                filter = Builders<Post>.Filter.And(filter, timeFilter);
            }
        }

        // Retrieve and return posts sorted by creation time (newest first)
        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<Result<List<Post>>> GetTimelinePostsAsync(string userId, string fromPostId = null, int limit = 10)
    {
        // Get IDs of user's friends and add the user's own ID
        var friendIdsResult = await friendshipService.GetFriendIdsAsync(userId);
        var relevantUserIds = friendIdsResult.Value;

        // Build the filter to get timeline posts
        var filter = Builders<Post>.Filter.Or(
            // Include all posts created by the user (regardless of privacy settings)
            Builders<Post>.Filter.Eq(f => f.UserId, userId),

            // Include posts from friends with appropriate privacy settings
            Builders<Post>.Filter.And(
                Builders<Post>.Filter.In(f => f.UserId, relevantUserIds),
                Builders<Post>.Filter.Or(
                    Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Friends),
                    Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends),
                    Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Everyone)
                )
            ),

            // Include posts where the user is specifically selected as a recipient
            Builders<Post>.Filter.And(
                Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.SelectedUsers),
                Builders<Post>.Filter.AnyEq(f => f.DiscoveryOptionSelectedUserIds, userId)
            )
        );

        // Add pagination filter if a reference post ID is provided
        if (!string.IsNullOrEmpty(fromPostId))
        {
            var fromPost = await _postCollection.Find(f => f.Id == fromPostId).FirstOrDefaultAsync();

            if (fromPost != null)
            {
                var timeFilter = Builders<Post>.Filter.Lt(f => f.CreatedAt, fromPost.CreatedAt);
                filter = Builders<Post>.Filter.And(filter, timeFilter);
            }
        }

        // Retrieve and return posts sorted by creation time (newest first)
        return await _postCollection
            .Find(filter)
            .Sort(Builders<Post>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<Result<long>> GetUserPostCountAsync(string userId, string requesterId = null)
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
        var filter = Builders<Post>.Filter.Eq(f => f.UserId, userId);
        // Add visibility filters based on privacy settings
        if (!isSelf)
        {
            var visibilityFilter = Builders<Post>.Filter.Or(
                // Public posts are always visible (even to non-logged in users)
                Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Everyone),
                // For logged in users who are friends, include posts with Friends or higher visibility
                !string.IsNullOrEmpty(requesterId) && areFriends
                    ? Builders<Post>.Filter.Or(
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                      )
                    : Builders<Post>.Filter.Empty,
                // For logged in users who are friends of friends, include posts with FriendsOfFriends visibility
                !string.IsNullOrEmpty(requesterId) && areFriendsOfFriends
                    ? Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    : Builders<Post>.Filter.Empty,
                // For logged in users included in SelectedUsers
                !string.IsNullOrEmpty(requesterId)
                    ? Builders<Post>.Filter.And(
                        Builders<Post>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Post>.Filter.AnyEq(f => f.DiscoveryOptionSelectedUserIds, requesterId)
                      )
                    : Builders<Post>.Filter.Empty
            );
            filter = Builders<Post>.Filter.And(filter, visibilityFilter);
        }

        // Count the number of posts that match the filter
        return await _postCollection.CountDocumentsAsync(filter);
    }
}
