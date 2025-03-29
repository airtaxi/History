using History.ApiService.Services.Interfaces;
using History.Commons.DataTypes;
using History.Commons.Enums;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace History.ApiService.Services;

//[BsonIgnoreExtraElements]
//public class Media
//{
//    [BsonId]
//    public string Id { get; set; }

//    public string FileName { get; set; }
//    public string BucketName { get; set; }
//}

public class MediaService(IMongoDatabase database) : IMediaService
{
    private readonly IMongoCollection<Media> _mediaCollection = database.GetCollection<Media>("Medias");

    public async Task<Media> GetMediaByIdAsync(string mediaId) => await _mediaCollection.Find(m => m.Id == mediaId).FirstOrDefaultAsync();

    public async Task<Media> CreateMediaAsync(MediaBucket bucketType, byte[] content)
    {
        // Upload media file to GridFS
        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = bucketType.ToString()
        });

        var media = new Media
        {
            FileName = Guid.NewGuid().ToString("N"),
            BucketType = bucketType
        };

        var id = await bucket.UploadFromBytesAsync(media.FileName, content);
        media.Id = id.ToString();

        await _mediaCollection.InsertOneAsync(media);
        return media;
    }

    public async Task<byte[]> FetchMediaFileContentAsync(MediaBucket bucketType, string fileName)
    {
        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = bucketType.ToString()
        });
        var file = await bucket.Find(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, fileName)).FirstOrDefaultAsync();
        if (file == null) return null;
        return await bucket.DownloadAsBytesAsync(file.Id);
    }

    public async Task DeleteMediaAsync(string mediaId)
    {
        var media = await GetMediaByIdAsync(mediaId);
        if (media == null) return;

        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = media.BucketType.ToString()
        });

        await bucket.DeleteAsync(mediaId);
        await _mediaCollection.DeleteOneAsync(m => m.Id == mediaId);
    }
}
