using History.ApiService.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace History.ApiService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MediaController(IMediaService mediaService) : ControllerBase
{
    [HttpGet("{mediaId}")]
    public async Task<IActionResult> GetMediaContent(string mediaId)
    {
        var media = await mediaService.GetMediaByIdAsync(mediaId);
        if (media == null) return NotFound("Media not found.");

        var mediaContent = await mediaService.FetchMediaFileContentAsync(media.BucketType, media.FileName);
        return File(mediaContent, "application/octet-stream");
    }
}