using History.Commons;
using History.Commons.DataTypes;
using History.Commons.Enums;

namespace History.ApiService.Services.Interfaces;

public interface IMediaService
{
    /// <summary>
    /// Get media by id.
    /// </summary>
    /// <param name="mediaId">The id of media to get</param>
    /// <returns>A task that represents the asynchronous operation. with result of media</returns>
    public Task<Result<Media>> GetMediaByIdAsync(string mediaId);

    /// <summary>
    /// Create media.
    /// </summary>
    /// <param name="bucketType">The bucket type of media</param>
    /// <param name="userId">The id of user who uploaded the media</param>
    /// <param name="content">The content of media</param>
    /// <returns>A task that represents the asynchronous operation. with result of created media</returns>
    public Task<Result<Media>> CreateMediaAsync(MediaBucket bucketType, string userId, byte[] content);

    /// <summary>
    /// Fetch media file content.
    /// </summary>
    /// <param name="bucketType">The bucket type of media</param>
    /// <param name="fileName">The file name of media</param>
    /// <returns>A task that represents the asynchronous operation. with result of media file content in byte array</returns>
    public Task<Result<byte[]>> FetchMediaFileContentAsync(MediaBucket bucketType, string fileName);

    /// <summary>
    /// Delete media.
    /// </summary>
    /// <param name="mediaId">The id of media to delete</param>
    /// <returns>A task that represents the asynchronous operation</returns>
    public Task<Result> DeleteMediaByMediaIdAsync(string mediaId);
}
