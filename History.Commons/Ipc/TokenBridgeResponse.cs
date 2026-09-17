namespace History.Commons.Ipc;

public sealed class TokenBridgeResponse
{
    public bool IsSuccess { get; set; }

    public string AccessToken { get; set; }

    public string RefreshToken { get; set; }

    public string KakaoStoryIdToken { get; set; }

    public string ErrorMessage { get; set; }
}
