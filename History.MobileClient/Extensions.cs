using NativeMedia;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient;

public static class Extensions
{
    public static string GenerateFileName(this IMediaFile file) => (file.NameWithoutExtension ?? string.Empty) + "." + file.Extension;
}
