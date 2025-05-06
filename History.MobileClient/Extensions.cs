using NativeMedia;

namespace History.MobileClient;

public static class Extensions
{
    public static string GenerateFileName(this IMediaFile file) => (file.NameWithoutExtension ?? string.Empty) + "." + file.Extension;
}
