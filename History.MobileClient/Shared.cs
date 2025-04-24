using History.Commons;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient;

public static class Shared
{
    public static ApiHandler ApiHandler { get; set; }
    public static string UserId { get; set; }
    public static DiscoveryOption LastUsedPostDiscoveryOption { get; set; }
}
