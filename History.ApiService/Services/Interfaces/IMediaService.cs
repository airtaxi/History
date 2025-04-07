using History.Commons;
using History.Commons.DataTypes;
using History.Commons.DataTypes.Contents;
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
    /// <param name="associatedId">The id associated with media</param>
    /// <param name="userId">The id of user who uploaded the media</param>
    /// <param name="content">The content of media</param>
    /// <returns>A task that represents the asynchronous operation. with result of created media</returns>
    public Task<Result<Media>> CreateMediaAsync(MediaBucket bucketType, string associatedId, string userId, byte[] content);


    /// <summary>
    /// Handles the upload of media contents and files, validating and processing them before storing.
    /// </summary>
    /// <param name="bucketType">Specifies the type of media storage to use for the upload.</param>
    /// <param name="associatedId">Identifies the entity associated with the media being uploaded.</param>
    /// <param name="userId">Represents the user initiating the upload process.</param>
    /// <param name="contents">Contains the list of content items to be uploaded, which may be replaced during the process.</param>
    /// <param name="files">Holds the collection of files to be uploaded, which must match the content items.</param>
    /// <returns>Indicates the success or failure of the upload operation.</returns>
    public Task<Result> HandleUploadContentsAsync(MediaBucket bucketType, string associatedId, string userId, IList<BaseContent> contents, IEnumerable<IFormFile> files);
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="associatedId"></param>
    /// <returns></returns>
    public Task<Result> DeleteByAssociatedIdAsync(string associatedId);

    /// <summary>
    /// 
    /// </summary>
    /// <param name="associatedIds"></param>
    /// <returns></returns>
    public Task<Result> DeleteByAssociatedIdsAsync(IEnumerable<string> associatedIds);

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
