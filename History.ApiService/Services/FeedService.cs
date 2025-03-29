using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;
using System.Collections.Generic;

namespace History.ApiService.Services;

public class FeedService(IMongoDatabase database, IFriendshipService friendshipService) : IFeedService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");
    private readonly IMongoCollection<Feed> _feedCollection = database.GetCollection<Feed>("Feeds");

    public async Task<Feed> GetFeedByIdAsync(string feedId) => await _feedCollection.Find(f => f.Id == feedId).FirstOrDefaultAsync();

    public async Task<List<Feed>> GetUserFeedsAsync(string requesterId, string userId, string fromFeedId = null, int limit = 10)
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

        // Base filter: feeds from the target user
        var filter = Builders<Feed>.Filter.Eq(f => f.AuthorUserId, userId);

        // Add visibility filters based on privacy settings
        if (!isSelf)
        {
            var visibilityFilter = Builders<Feed>.Filter.Or(
                // Public feeds are always visible (even to non-logged in users)
                Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Everyone),

                // For logged in users who are friends, include feeds with Friends or higher visibility
                !string.IsNullOrEmpty(requesterId) && areFriends
                    ? Builders<Feed>.Filter.Or(
                        Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Friends),
                        Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                      )
                    : Builders<Feed>.Filter.Empty,

                // For logged in users who are friends of friends, include feeds with FriendsOfFriends visibility
                !string.IsNullOrEmpty(requesterId) && areFriendsOfFriends
                    ? Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends)
                    : Builders<Feed>.Filter.Empty,

                // For logged in users included in SelectedUsers
                !string.IsNullOrEmpty(requesterId)
                    ? Builders<Feed>.Filter.And(
                        Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.SelectedUsers),
                        Builders<Feed>.Filter.AnyEq(f => f.DiscoveryOptionSelectedUserIds, requesterId)
                      )
                    : Builders<Feed>.Filter.Empty
            );

            filter = Builders<Feed>.Filter.And(filter, visibilityFilter);
        }

        // Add pagination filter
        if (!string.IsNullOrEmpty(fromFeedId))
        {
            var fromFeed = await _feedCollection.Find(f => f.Id == fromFeedId).FirstOrDefaultAsync();
            if (fromFeed != null)
            {
                var timeFilter = Builders<Feed>.Filter.Lt(f => f.CreatedAt, fromFeed.CreatedAt);
                filter = Builders<Feed>.Filter.And(filter, timeFilter);
            }
        }

        // Retrieve and return feeds sorted by creation time (newest first)
        return await _feedCollection
            .Find(filter)
            .Sort(Builders<Feed>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }

    public async Task<List<Feed>> GetTimelineFeedsAsync(string userId, string fromFeedId = null, int limit = 10)
    {
        // Check if the user exists
        var user = await _userCollection.Find(u => u.Id == userId).FirstOrDefaultAsync();
        if (user == null) return [];

        // Get IDs of user's friends and add the user's own ID
        var relevantUserIds = await friendshipService.GetFriendIdsAsync(userId);
        relevantUserIds.Add(userId);

        // Build the filter to get timeline feeds
        var filter = Builders<Feed>.Filter.Or(
            // Include all feeds created by the user (regardless of privacy settings)
            Builders<Feed>.Filter.Eq(f => f.AuthorUserId, userId),

            // Include feeds from friends with appropriate privacy settings
            Builders<Feed>.Filter.And(
                Builders<Feed>.Filter.In(f => f.AuthorUserId, relevantUserIds),
                Builders<Feed>.Filter.Or(
                    Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Friends),
                    Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.FriendsOfFriends),
                    Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.Everyone)
                )
            ),

            // Include feeds where the user is specifically selected as a recipient
            Builders<Feed>.Filter.And(
                Builders<Feed>.Filter.Eq(f => f.DiscoveryOption, DiscoveryOption.SelectedUsers),
                Builders<Feed>.Filter.AnyEq(f => f.DiscoveryOptionSelectedUserIds, userId)
            )
        );

        // Add pagination filter if a reference feed ID is provided
        if (!string.IsNullOrEmpty(fromFeedId))
        {
            var fromFeed = await _feedCollection.Find(f => f.Id == fromFeedId).FirstOrDefaultAsync();

            if (fromFeed != null)
            {
                var timeFilter = Builders<Feed>.Filter.Lt(f => f.CreatedAt, fromFeed.CreatedAt);
                filter = Builders<Feed>.Filter.And(filter, timeFilter);
            }
        }

        // Retrieve and return feeds sorted by creation time (newest first)
        return await _feedCollection
            .Find(filter)
            .Sort(Builders<Feed>.Sort.Descending(f => f.CreatedAt))
            .Limit(limit)
            .ToListAsync();
    }
}
