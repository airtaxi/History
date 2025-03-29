using History.Commons.DataTypes;

namespace History.ApiService.Services.Interfaces;

public interface IFeedService
{
    /// <summary>
    /// Get feed by id.
    /// </summary>
    /// <param name="feedId">The id of feed to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of feed</returns>
    public Task<Feed> GetFeedByIdAsync(string feedId);

    /// <summary>
    /// Get timeline feeds of user.
    /// </summary>
    /// <param name="userId">The id of user to get timeline feeds</param>
    /// <param name="fromFeedId">The id of feed to start from</param>
    /// <param name="limit">The limit of feeds to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of feeds</returns>
    public Task<List<Feed>> GetTimelineFeedsAsync(string userId, string fromFeedId = null, int limit = 10);

    /// <summary>
    /// Get feeds of user.
    /// </summary>
    /// <param name="requesterId">The id of user who requests feeds</param>
    /// <param name="userId">The id of user to get feeds</param>
    /// <param name="fromFeedId">The id of feed to start from</param>
    /// <param name="limit">The limit of feeds to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of feeds</returns>
    public Task<List<Feed>> GetUserFeedsAsync(string requesterId, string userId, string fromFeedId = null, int limit = 10);
}
