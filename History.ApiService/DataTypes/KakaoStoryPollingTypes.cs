using System.Text.Json.Serialization;

namespace History.ApiService.DataTypes;

/// <summary>
/// Kakao Story notification (subset of the /a/notifications response).
/// </summary>
public class KakaoStoryNotification
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; }

    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("key")]
    public string Key { get; set; }

    [JsonPropertyName("scheme")]
    public string Scheme { get; set; }

    [JsonPropertyName("thumbnail_url")]
    public string ThumbnailUrl { get; set; }

    [JsonPropertyName("is_new")]
    public bool IsNew { get; set; }

    [JsonPropertyName("decorators")]
    public List<KakaoStoryNotificationDecorator> Decorators { get; set; }

    [JsonPropertyName("emotion")]
    public string Emotion { get; set; }
}

/// <summary>
/// Decorator of a Kakao Story notification (subset used for the favorite friend
/// filter, mirroring the client's KakaoStoryNotificationPoller).
/// </summary>
public class KakaoStoryNotificationDecorator
{
    [JsonPropertyName("text")]
    public string Text { get; set; }
}

/// <summary>
/// Batch request sent to the Cloudflare Worker proxy: a list of { url, idToken }
/// pairs. The worker fetches each URL from Cloudflare's IP pool with the KAuth
/// Authorization header and returns the raw responses without parsing them.
/// </summary>
public class KakaoStoryWorkerBatchRequest
{
    public List<KakaoStoryWorkerRequest> Requests { get; set; }
}

public class KakaoStoryWorkerRequest
{
    public string Url { get; set; }

    public string IdToken { get; set; }
}

public class KakaoStoryWorkerBatchResponse
{
    public List<KakaoStoryWorkerResponse> Responses { get; set; }
}

public class KakaoStoryWorkerResponse
{
    public int Status { get; set; }

    public string Body { get; set; }
}
