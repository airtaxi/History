using History.Commons.DataTypes.RequestDtos;
using History.Commons.Interfaces;
using RestSharp;

namespace History.Commons.Api.User;

public class UpdateBirthday : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/birthday";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public UpdateBirthday(DateTime? birthday) => Body = new UpdateUserBirthdayRequestDto()
    {
        Birthday = birthday
    };
}
