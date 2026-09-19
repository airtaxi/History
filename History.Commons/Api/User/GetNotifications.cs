using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;

namespace History.Commons.Api.User;

public class GetNotifications : IBaseRequest<List<NotificationResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/notifications";
    public HttpRequestMethod Method => HttpRequestMethod.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetNotifications(string fromNotificationId = null, int limit = 30)
    {
        QueryParameters["limit"] = limit.ToString();
        if (fromNotificationId != null) QueryParameters["from"] = fromNotificationId;
    }
}
