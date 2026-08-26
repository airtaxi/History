using History.Commons.Enums;

namespace History.WindowsClient.Models;

public sealed record RegisterPageParameters(string IdToken, SocialService SocialService, string Name = null);
