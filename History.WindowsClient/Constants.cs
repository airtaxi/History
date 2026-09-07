using System;
using System.Collections.Generic;
using System.Text;

namespace History.WindowsClient;

public static class Constants
{
    // File type filters for the image open picker (attachments, profile/background media, message images).
    public static readonly string[] ImageFileTypeFilters = [".png", ".apng", ".jpg", ".jpeg", ".webp", ".gif", ".tif", ".tiff"];

    // File type filters for the video open picker; the server converts webp/gif to video on its own.
    public static readonly string[] VideoFileTypeFilters = [".mp4", ".mov", ".avi", ".mkv", ".webm"];

    // Combined image and video filters for the media open picker.
    public static readonly string[] MediaFileTypeFilters = [.. ImageFileTypeFilters, .. VideoFileTypeFilters];

    // Paging size shared by the timeline, profile, and notification feeds.
    public const int PageSize = 30;

    // Shared dialog title for error messages.
    public const string ErrorTitle = "오류";

    // Sticker load failure message shown when the picked sticker image cannot be fetched.
    public const string StickerLoadErrorMessage = "스티커 이미지를 불러올 수 없습니다.";
}
