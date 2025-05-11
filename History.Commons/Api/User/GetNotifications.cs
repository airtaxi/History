using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetNotifications : IBaseRequest<List<NotificationResponseDto>>, IAuthRequiredRequest, IRequestWithUrlParameters
{
    public string Path => "/api/user/notifications";
    public Method Method => Method.Get;
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public GetNotifications(string fromNotificationId = null, int limit = 30)
    {
        UrlParameters["limit"] = limit.ToString();
        if (fromNotificationId != null) UrlParameters["from"] = fromNotificationId;
    }
}
