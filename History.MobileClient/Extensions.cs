#if WINDOWS
using History.MobileClient.Helpers;
using Microsoft.Maui.Storage;
#else
using NativeMedia;
#endif

namespace History.MobileClient;

public static class Extensions
{
#if WINDOWS
    public static string GenerateFileName(this FileResult file) => Path.GetFileName(file.FullPath ?? file.FileName);
#else
    public static string GenerateFileName(this IMediaFile file) => (file.NameWithoutExtension ?? string.Empty) + "." + file.Extension;
#endif
}
