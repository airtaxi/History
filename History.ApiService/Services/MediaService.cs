using History.ApiService.DataTypes;
using History.ApiService.Helpers;
using History.ApiService.Services.Interfaces;
using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace History.ApiService.Services;

public class MediaService(IMongoDatabase database) : IMediaService
{
    private readonly IMongoCollection<Media> _mediaCollection = database.GetCollection<Media>("Medias");

    /// <inheritdoc />
    public async Task<Result<Media>> GetMediaByIdAsync(string mediaId)
    {
        var media = await _mediaCollection.Find(m => m.Id == mediaId).FirstOrDefaultAsync();
        if (media == null) return (ErrorType.NotFound, "미디어를 찾을 수 없습니다.");
        else return media;
    }

    /// <inheritdoc />
    public async Task<Result<Media>> CreateMediaAsync(MediaBucket bucketType, string associatedId, string userId, byte[] content, string mimeType, string thumbnailMediaId = null)
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
            MimeType = mimeType,
            ThumbnailMediaId = thumbnailMediaId,
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
            // Validate all files exist first
            foreach (var uploadContent in uploadContents)
            {
                var fileExists = files.FirstOrDefault(f => f.FileName == uploadContent.FileName);
                if (fileExists == null) return Result.Failure(ErrorType.BadRequest, "파일이 존재하지 않습니다.");
            }

            // Process files in parallel while maintaining order
            var processingTasks = uploadContents.Select(async (uploadContent, index) =>
            {
                var file = files.FirstOrDefault(f => f.FileName == uploadContent.FileName);

                try
                {
                    // Read file bytes
                    using var writeStream = new MemoryStream();
                    await file.CopyToAsync(writeStream);
                    var originalFileBytes = writeStream.ToArray();

                    byte[] bytes;
                    var contentType = file.ContentType;
                    var isImage = file.ContentType.StartsWith("image/");
                    var wasImage = isImage;

                    // Convert image if needed
                    if (isImage)
                    {
                        var convertResult = MediaEncodingHelper.ConvertImage(originalFileBytes, true, maxWidth: 16383, maxHeight: 16383); // WebP supports up to 16383x16383
                        bytes = convertResult.Data;
                        contentType = convertResult.MimeType;
                        isImage = !convertResult.IsVideo;
                    }
                    else
                    {
                        var convertResult = MediaEncodingHelper.ConvertVideo(originalFileBytes, 1080);
                        bytes = convertResult.Data;
                        contentType = convertResult.MimeType;
                    }

                    // Check file size
                    var isOverSize = bytes.Length > (wasImage ? CommonConstants.MaxImageUploadFileSize : CommonConstants.MaxUploadFileSize); // 100MB, 60MB for images and videos respectively
                    if (isOverSize)
                    {
                        return (Index: index, Result: Result.Failure(ErrorType.BadRequest, $"{file.FileName}: 파일 크기가 너무 큽니다."), MediaContent: null);
                    }

                    // Generate thumbnail
                    string thumbnailId;
                    try
                    {
                        MediaConvertResult thumbnailConvertResult;
                        if (wasImage)
                        {
                            thumbnailConvertResult = MediaEncodingHelper.ConvertImage(originalFileBytes, false, maxWidth:512);
                        }
                        else
                        {
                            thumbnailConvertResult = MediaEncodingHelper.GenerateThumbnailFromVideo(originalFileBytes, 512);
                        }

                        var thumbnailBytes = thumbnailConvertResult.Data;
                        var thumbnailContentType = thumbnailConvertResult.MimeType;

                        var thumbnailResult = await CreateMediaAsync(bucketType, associatedId, userId, thumbnailBytes, thumbnailContentType);
                        if (thumbnailResult.IsFailure)
                        {
                            return (Index: index, Result: thumbnailResult.CastFailure(), MediaContent: null);
                        }

                        thumbnailId = thumbnailResult.Value.Id;
                    }
                    catch (Exception exception)
                    {
                        return (Index: index,
                               Result: Result.Failure(ErrorType.ProgramError, $"지원하지 않는 미디어 형식입니다.\n코드: {exception.Message} {exception.StackTrace}"),
                               MediaContent: null);
                    }

                    // Upload main media
                    var mediaResult = await CreateMediaAsync(bucketType, associatedId, userId, bytes, contentType, thumbnailId);
                    if (mediaResult.IsFailure)
                    {
                        return (Index: index, Result: mediaResult.CastFailure(), MediaContent: null);
                    }

                    // Create MediaContent
                    var mediaContent = new MediaContent
                    {
                        MediaId = mediaResult.Value.Id,
                        ThumbnailMediaId = thumbnailId,
                        Description = uploadContent.Description,
                        MimeType = contentType,
                        IsSpoiler = uploadContent.IsSpoiler
                    };

                    return (Index: index, Result: Result.Success(), MediaContent: mediaContent);
                }
                catch (Exception ex)
                {
                    return (Index: index,
                           Result: Result.Failure(ErrorType.ProgramError, $"파일 처리 중 오류 발생: {ex.Message}"),
                           MediaContent: null);
                }
            }).ToList();

            // Wait for all tasks to complete
            var results = await Task.WhenAll(processingTasks);

            // Check if any task failed
            var firstFailure = results.FirstOrDefault(r => r.Result.IsFailure);
            if (firstFailure != default) return firstFailure.Result;

            // Replace UploadContent with MediaContent in the original order
            // Process in reverse order to maintain correct indices
            foreach (var result in results.OrderByDescending(r => r.Index))
            {
                var uploadContent = uploadContents[result.Index];
                var uploadContentIndex = contents.IndexOf(uploadContent);

                contents.Insert(uploadContentIndex, result.MediaContent);
                contents.Remove(uploadContent);
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMediaByAssociatedIdAsync(string associatedId)
    {
        var medias = await _mediaCollection.Find(m => m.AssociatedId == associatedId).ToListAsync();

        foreach (var m in medias)
        {
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = m.BucketType.ToString()
            });

            await bucket.DeleteAsync(ObjectId.Parse(m.Id));
        }

        await _mediaCollection.DeleteManyAsync(m => m.AssociatedId == associatedId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMediasByAssociatedIdsAsync(IEnumerable<string> associatedIds)
    {
        var media = await _mediaCollection.Find(m => associatedIds.Contains(m.AssociatedId)).ToListAsync();

        foreach (var m in media)
        {
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = m.BucketType.ToString()
            });

            await bucket.DeleteAsync(ObjectId.Parse(m.Id));
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
        if (file == null) return (ErrorType.NotFound, "미디어를 찾을 수 없습니다.");

        return await bucket.DownloadAsBytesAsync(file.Id);
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMediasByUserIdAsync(string userId)
    {
        var medias = await _mediaCollection.Find(m => m.UserId == userId).ToListAsync();

        foreach (var m in medias)
        {
            var bucket = new GridFSBucket(database, new GridFSBucketOptions
            {
                BucketName = m.BucketType.ToString()
            });
            await bucket.DeleteAsync(ObjectId.Parse(m.Id));
        }

        await _mediaCollection.DeleteManyAsync(m => m.UserId == userId);

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> DeleteMediaByIdAsync(string mediaId)
    {
        var mediaResult = await GetMediaByIdAsync(mediaId);
        if (mediaResult.IsFailure) return mediaResult;

        var bucket = new GridFSBucket(database, new GridFSBucketOptions
        {
            BucketName = mediaResult.Value.BucketType.ToString()
        });

        await bucket.DeleteAsync(ObjectId.Parse(mediaId));
        await _mediaCollection.DeleteOneAsync(m => m.Id == mediaId);

        if (mediaResult.Value.ThumbnailMediaId != null)
        {
            var thumbnailMediaResult = await GetMediaByIdAsync(mediaResult.Value.ThumbnailMediaId);
            if (thumbnailMediaResult.IsSuccess)
            {
                if (thumbnailMediaResult.Value.BucketType != mediaResult.Value.BucketType)
                {
                    bucket = new GridFSBucket(database, new GridFSBucketOptions
                    {
                        BucketName = thumbnailMediaResult.Value.BucketType.ToString()
                    });
                }

                await bucket.DeleteAsync(ObjectId.Parse(thumbnailMediaResult.Value.Id));
                await _mediaCollection.DeleteOneAsync(m => m.Id == thumbnailMediaResult.Value.Id);
            }
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> HandleWithdraw(string userId) => await DeleteMediasByUserIdAsync(userId);
}
