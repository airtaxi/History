using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
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
    public async Task<Result<Media>> CreateMediaAsync(MediaBucket bucketType, string associatedId, string userId, byte[] content)
    {
        // Upload media file to GridFS
        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = bucketType.ToString()
        });

        var media = new Media
        {
            FileName = Guid.NewGuid().ToString("N"),
            UserId = userId,
            AssociatedId = associatedId,
            MediaSize = content.Length,
            BucketType = bucketType,
            CreatedAt = DateTime.UtcNow
        };

        var id = await bucket.UploadFromBytesAsync(media.FileName, content);
        media.Id = id.ToString();

        await _mediaCollection.InsertOneAsync(media);
        return media;
    }

    /// <inheritdoc />
    public async Task<Result> HandleUploadContentsAsync(MediaBucket bucketType, string associatedId, string userId, IList<BaseContent> contents, IEnumerable<IFormFile> files)
    {
        var uploadContents = contents.OfType<UploadContent>().ToList();

        if (uploadContents.Count > 0)
        {
            foreach (var uploadContent in uploadContents)
            {
                var fileExists = files.FirstOrDefault(f => f.FileName == uploadContent.FileName);
                if (fileExists == null) return Result.Failure(ErrorType.BadRequest, "파일이 존재하지 않습니다.");
            }

            // Upload files
            foreach (var uploadContent in uploadContents)
            {
                var file = files.FirstOrDefault(f => f.FileName == uploadContent.FileName);

                using var writeStream = new MemoryStream();
                await file.CopyToAsync(writeStream);
                var bytes = writeStream.ToArray();

                var mediaResult = await CreateMediaAsync(bucketType, associatedId, userId, bytes);
                if (mediaResult.IsFailure) return mediaResult.CastFailure();

                // Replace UploadContent with MediaContent By remove after insert
                var mediaContent = new MediaContent
                {
                    MediaId = mediaResult.Value.Id,
                    Description = uploadContent.Description
                };

                contents.Insert(contents.IndexOf(uploadContent), mediaContent);
                contents.Remove(uploadContent);
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteByAssociatedIdAsync(string associatedId)
    {
        var media = await _mediaCollection.Find(m => m.AssociatedId == associatedId).FirstOrDefaultAsync();
        if (media == null) return Result.Failure(ErrorType.NotFound, "미디어를 찾을 수 없습니다.");

        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = media.BucketType.ToString()
        });

        await bucket.DeleteAsync(media.Id);

        await _mediaCollection.DeleteOneAsync(m => m.Id == media.Id);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteByAssociatedIdsAsync(IEnumerable<string> associatedIds)
    {
        var media = await _mediaCollection.Find(m => associatedIds.Contains(m.AssociatedId)).ToListAsync();
        if (media == null || media.Count == 0) return Result.Failure(ErrorType.NotFound, "미디어를 찾을 수 없습니다.");

        foreach (var m in media)
        {
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = m.BucketType.ToString()
            });

            await bucket.DeleteAsync(m.Id);
        }

        await _mediaCollection.DeleteManyAsync(m => associatedIds.Contains(m.AssociatedId));
        return Result.Success();
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

        return Result.Success();
    }
}
