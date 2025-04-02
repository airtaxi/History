using History.ApiService.Services.Interfaces;
using History.Commons;
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

    /// <inheritdoc />
    public async Task<Result<Media>> GetMediaByIdAsync(string mediaId)
    {
        var media = await _mediaCollection.Find(m => m.Id == mediaId).FirstOrDefaultAsync();
        if (media == null) return Result<Media>.Failure(ErrorType.NotFound, "미디어를 찾을 수 없습니다.");
        else return media;
    }

    /// <inheritdoc />
    public async Task<Result<Media>> CreateMediaAsync(MediaBucket bucketType, byte[] content)
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

    /// <inheritdoc />
    public async Task<Result<byte[]>> FetchMediaFileContentAsync(MediaBucket bucketType, string fileName)
    {
        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = bucketType.ToString()
        });
        var file = await bucket.Find(Builders<GridFSFileInfo>.Filter.Eq(x => x.Filename, fileName)).FirstOrDefaultAsync();
        if (file == null) return Result<byte[]>.Failure(ErrorType.NotFound, "미디어를 찾을 수 없습니다.");

        return await bucket.DownloadAsBytesAsync(file.Id);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMediaByMediaIdAsync(string mediaId)
    {
        var mediaResult = await GetMediaByIdAsync(mediaId);
        if (mediaResult.IsFailure) return mediaResult;

        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = mediaResult.Value.BucketType.ToString()
        });

        await bucket.DeleteAsync(mediaId);
        await _mediaCollection.DeleteOneAsync(m => m.Id == mediaId);

        return null;
    }
}
