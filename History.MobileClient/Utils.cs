using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient;

public static class Utils
{
    public static string GenerateMediaUri(string mediaId)
    {
        if (mediaId == null) return null;

        return $"https://api.history.cenox.io/api/media/{mediaId}";
    }
}
