using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Dto;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class UserService(IMongoDatabase database, IMediaService mediaService, IFriendshipService friendshipService) : IUserService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");

    /// <inheritdoc />
    public async Task<Result> CreateUserAsync(User user)
    {
        var isUserCollectionEmpty = await _userCollection.CountDocumentsAsync(FilterDefinition<User>.Empty) == 0;
        if (isUserCollectionEmpty) user.Rank = Rank.Admin;
        else user.Rank = Rank.Unauthorized;

        await _userCollection.InsertOneAsync(user);

        return null;
    }

    /// <inheritdoc />
    public async Task<Result<User>> GetUserByIdAsync(string id)
    {
        var user = await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (user == null) return ErrorType.NotFound;

        return user;
    }

    /// <inheritdoc />
    public async Task<Result<List<User>>> GetUsersByIdsAsync(IEnumerable<string> userIds) => await _userCollection.Find(u => userIds.Contains(u.Id)).ToListAsync();

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateDescriptionAsync(string userId, string description)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Description, description);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateBirthdayAsync(string userId, DateTime? birthday)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Birthday, birthday);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateNicknameAsync(string userId, string nickname)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Nickname, nickname);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateProfileMediaAsync(string userId, byte[] image)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.Error != null) return userResult.Error;
        else if (userResult == null) return ErrorType.NotFound;

        if (userResult.Value.ProfileMediaId != null) await mediaService.DeleteMediaByMediaIdAsync(userResult.Value.ProfileMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Unset(u => u.ProfileMediaId);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
        }
        else
        {
            var media = await mediaService.CreateMediaAsync(MediaBucket.ProfileMedia, image);
            if (media.Error != null) return media.Error;

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.ProfileMediaId, media.Value.Id);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
        }
    }

    /// <inheritdoc />
    public async Task<Result<bool>> UpdateBackgroundMediaAsync(string userId, byte[] image)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.Error != null) return userResult.Error;
        else if (userResult == null) return ErrorType.NotFound;

        if (userResult.Value.BackgroundMediaId != null) await mediaService.DeleteMediaByMediaIdAsync(userResult.Value.BackgroundMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Unset(u => u.BackgroundMediaId);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
        }
        else
        {
            var mediaResult = await mediaService.CreateMediaAsync(MediaBucket.BackgroundMedia, image);
            if (mediaResult.Error != null) return mediaResult.Error;

            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.BackgroundMediaId, mediaResult.Value.Id);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0 ? true : ErrorType.NotFound;
        }
    }
    /// <inheritdoc/>
    public async Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(User user, string requesterId = null)
    {
        var result = new UserResponseDto(user);

        var friendshipResult = await friendshipService.GetFriendshipAsync(user.Id, requesterId);
        if (friendshipResult.IsFailure) return friendshipResult.Error;

        result.Friendship = friendshipResult.Value;

        return result;
    }

    /// <inheritdoc/>
    public async Task<Result<UserResponseDto>> GenerateUserResponseDtoAsync(string userId, string requesterId = null)
    {
        var userResult = await GetUserByIdAsync(userId);
        if (userResult.IsFailure) return userResult.Error;

        return await GenerateUserResponseDtoAsync(userResult, requesterId);
    }

    /// <inheritdoc/>
    public async Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<User> users, string requesterId = null)
    {
        var results = users.Select(x => new UserResponseDto(x)).ToList();

        var friendshipsResult = await friendshipService.GetAllFriendshipsAsync(requesterId);
        if (friendshipsResult.IsFailure) return friendshipsResult.Error;

        foreach (var result in results) result.Friendship = friendshipsResult.Value.FirstOrDefault(x => x.FriendId == requesterId);

        return results;
    }

    /// <inheritdoc/>
    public async Task<Result<List<UserResponseDto>>> GenerateUserResponseDtosAsync(IEnumerable<string> userIds, string requesterId = null)
    {
        var usersResult = await GetUsersByIdsAsync(userIds);
        if (usersResult.IsFailure) return usersResult.Error;

        return await GenerateUserResponseDtosAsync(usersResult.Value, requesterId);
    }
}
