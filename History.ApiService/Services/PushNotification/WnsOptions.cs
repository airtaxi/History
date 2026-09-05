namespace History.ApiService.Services.PushNotification;

public class WnsOptions
{
    public const string SectionName = "Wns";

    public string TokenEndpoint { get; set; } = "https://login.live.com/accesstoken.srf";
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Scope { get; set; } = "notify.windows.com";
}