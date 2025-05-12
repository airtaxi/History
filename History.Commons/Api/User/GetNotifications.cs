using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class GetNotifications : IBaseRequest<List<NotificationResponseDto>>, IAuthRequiredRequest, IRequestWithQueryParameters
{
    public string Path => "/api/user/notifications";
    public Method Method => Method.Get;
    public Dictionary<string, string> QueryParameters { get; set; } = [];

    public GetNotifications(string fromNotificationId = null, int limit = 30)
    {
        QueryParameters["limit"] = limit.ToString();
        if (fromNotificationId != null) QueryParameters["from"] = fromNotificationId;
    }
}
