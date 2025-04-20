using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.User;

public class UpdateNickname : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/nickname";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public UpdateNickname(string nickname) => Body = new UpdateUserNicknameRequestDto
    {
        Nickname = nickname
    };
}
