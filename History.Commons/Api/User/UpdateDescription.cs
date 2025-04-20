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

public class UpdateDescription : IAuthRequiredRequest, IRequestWithBody
{
    public string Path => "/api/user/description";
    public Method Method => Method.Put;
    public object Body { get; set; }

    public UpdateDescription(string description) => Body = new UpdateUserDescriptionRequestDto
    {
        Description = description
    };
}
