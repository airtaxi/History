using History.ApiService.Services.Interfaces;
using History.Commons;
using Microsoft.AspNetCore.Mvc;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    /// <summary>
    /// Get media content by id. 
    /// </summary>
    /// <param name="mediaId">The id of media to get content</param>
    /// <returns>A task that represents the asynchronous operation. with media content in byte array</returns>
    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMediaContent(string mediaId)
    {
        var mediaResult = await mediaService.GetMediaByIdAsync(mediaId);
        if (mediaResult.Error == ErrorType.NotFound) return NotFound(mediaResult.ErrorMessage);

        var mediaContent = await mediaService.FetchMediaFileContentAsync(mediaResult.Value.BucketType, mediaResult.Value.FileName);
        if (mediaContent.Error == ErrorType.NotFound) return NotFound(mediaContent.ErrorMessage);

        return File(mediaContent, "application/octet-stream");
    }
}