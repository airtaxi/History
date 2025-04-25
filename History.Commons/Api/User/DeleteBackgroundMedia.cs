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

public class DeleteBackgroundMedia : IAuthRequiredRequest
{
    public string Path => "/api/user/background-media";
    public Method Method => Method.Delete;
}
