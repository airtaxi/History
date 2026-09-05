namespace History.ApiService.Services.PushNotification;

public class WnsOptions
{
    public const string SectionName = "Wns";

    public string TokenEndpoint { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Scope { get; set; } = "https://wns.windows.com/.default";
}