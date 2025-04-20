
using History.Commons.DataTypes;
using MongoDB.Driver;

namespace History.ApiService.Services
{
    public class DatabaseInitService(IMongoDatabase database, ILogger<DatabaseInitService> logger) : IHostedService
    {
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Initializing database...");

            var userCollection = database.GetCollection<User>("Users");
            var postCollection = database.GetCollection<Post>("Posts");
            var friendshipCollection = database.GetCollection<Friendship>("Friendships");
            var commentCollection = database.GetCollection<Comment>("Comments");
            var mediaCollection = database.GetCollection<Media>("Medias");
            var refreshTokenCollection = database.GetCollection<RefreshToken>("RefreshTokens");

            // Create indexes
            logger.LogInformation("Creating indexes...");

            logger.LogInformation("Creating indexes for Users collection...");
            await userCollection.Indexes.CreateOneAsync(new CreateIndexModel<User>(Builders<User>.IndexKeys.Ascending(x => x.Nickname)), cancellationToken: cancellationToken);

            logger.LogInformation("Creating indexes for Posts collection...");
            await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
            await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.ParentPostId)), cancellationToken: cancellationToken);
            await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Ascending(x => x.SearchIndex)), cancellationToken: cancellationToken);
            await postCollection.Indexes.CreateOneAsync(new CreateIndexModel<Post>(Builders<Post>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

            logger.LogInformation("Creating indexes for Friendships collection...");
            await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
            await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.FriendId)), cancellationToken: cancellationToken);
            await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.Status)), cancellationToken: cancellationToken);
            await friendshipCollection.Indexes.CreateOneAsync(new CreateIndexModel<Friendship>(Builders<Friendship>.IndexKeys.Ascending(x => x.CreatedAt)), cancellationToken: cancellationToken);

            logger.LogInformation("Creating indexes for Comments collection...");
            await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Ascending(x => x.PostId)), cancellationToken: cancellationToken);
            await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
            await commentCollection.Indexes.CreateOneAsync(new CreateIndexModel<Comment>(Builders<Comment>.IndexKeys.Descending(x => x.CreatedAt)), cancellationToken: cancellationToken);

            logger.LogInformation("Creating indexes for Medias collection...");
            await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.FileName)), cancellationToken: cancellationToken);
            await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
            await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.AssociatedId)), cancellationToken: cancellationToken);
            await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.BucketType)), cancellationToken: cancellationToken);
            await mediaCollection.Indexes.CreateOneAsync(new CreateIndexModel<Media>(Builders<Media>.IndexKeys.Ascending(x => x.CreatedAt)), cancellationToken: cancellationToken);

            logger.LogInformation("Creating indexes for RefreshTokens collection...");
            await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(x => x.UserId)), cancellationToken: cancellationToken);
            await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(x => x.Token)), cancellationToken: cancellationToken);
            await refreshTokenCollection.Indexes.CreateOneAsync(new CreateIndexModel<RefreshToken>(Builders<RefreshToken>.IndexKeys.Ascending(x => x.CreatedAt)), cancellationToken: cancellationToken);

            logger.LogInformation("Indexes created successfully.");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
