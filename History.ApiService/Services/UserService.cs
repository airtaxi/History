using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;

namespace History.ApiService.Services;

public class UserService(IMongoDatabase database, IMediaService mediaService) : IUserService
{
    private readonly IMongoCollection<User> _userCollection = database.GetCollection<User>("Users");

    public async Task CreateUserAsync(User user)
    {
        var isUserCollectionEmpty = await _userCollection.CountDocumentsAsync(FilterDefinition<User>.Empty) == 0;
        if (isUserCollectionEmpty) user.Rank = Rank.Admin;
        else user.Rank = Rank.User;

        await _userCollection.InsertOneAsync(user);
    }

    public async Task<User> GetUserByIdAsync(string id) => await _userCollection.Find(u => u.Id == id).FirstOrDefaultAsync();

    public async Task<bool> UpdateDescriptionAsync(string userId, string description)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Description, description);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
    }

    public async Task<bool> UpdateBirthdayAsync(string userId, DateTime? birthday)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Birthday, birthday);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
    }

    public async Task<bool> UpdateNicknameAsync(string userId, string nickname)
    {
        var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
        var update = Builders<User>.Update.Set(u => u.Nickname, nickname);
        return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
    }

    public async Task<bool> UpdateProfileMediaAsync(string userId, byte[] image)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;

        if (user.ProfileMediaId != null) await mediaService.DeleteMediaAsync(user.ProfileMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Unset(u => u.ProfileMediaId);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
        }
        else
        {
            var media = await mediaService.CreateMediaAsync(MediaBucket.ProfileMedia, image);
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.ProfileMediaId, media.Id);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
        }
    }

    public async Task<bool> UpdateBackgroundMediaAsync(string userId, byte[] image)
    {
        var user = await GetUserByIdAsync(userId);
        if (user == null) return false;

        if (user.BackgroundMediaId != null) await mediaService.DeleteMediaAsync(user.BackgroundMediaId);

        if (image == null)
        {
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Unset(u => u.BackgroundMediaId);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
        }
        else
        {
            var media = await mediaService.CreateMediaAsync(MediaBucket.BackgroundMedia, image);
            var filter = Builders<User>.Filter.Eq(u => u.Id, userId);
            var update = Builders<User>.Update.Set(u => u.BackgroundMediaId, media.Id);
            return (await _userCollection.UpdateOneAsync(filter, update)).MatchedCount > 0;
        }
    }
}
