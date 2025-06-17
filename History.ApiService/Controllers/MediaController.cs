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
    [ProducesResponseType<byte[]>(200)]
    [ProducesResponseType<string>(404)]
    [ProducesResponseType<string>(500)]
    public async Task<IActionResult> GetMediaContent(string mediaId)
    {
        if(mediaId.Contains('.')) mediaId = mediaId.Split(".")[0]; // Remove file extension if exists

        var mediaResult = await mediaService.GetMediaByIdAsync(mediaId);
        if (mediaResult.Error == ErrorType.NotFound) return NotFound(mediaResult.ErrorMessage);
        else if (mediaResult.IsFailure) return StatusCode(500, mediaResult.FullErrorMessage);

        var mediaContent = await mediaService.FetchMediaFileContentAsync(mediaResult.Value.BucketType, mediaResult.Value.FileName);
        if (mediaContent.Error == ErrorType.NotFound) return NotFound(mediaContent.ErrorMessage);
        else if (mediaContent.IsFailure) return StatusCode(500, mediaContent.FullErrorMessage);

        
        Response.Headers.Append("Cache-Control", "public, max-age=604800");
        Response.Headers.Append("Expires", DateTime.UtcNow.AddDays(1).ToString("R"));
        Response.Headers.Append("Last-Modified", DateTime.UtcNow.ToString("R"));

        var etag = $"\"{mediaId}\"";
        Response.Headers.Append("ETag", etag);

        if (Request.Headers.ContainsKey("If-None-Match") &&
            Request.Headers["If-None-Match"].ToString() == etag)
        {
            return StatusCode(304);
        }

        return File(mediaContent, mediaResult.Value.MimeType, true);
    }
}