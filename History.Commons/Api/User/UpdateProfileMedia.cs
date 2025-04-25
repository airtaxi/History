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

public class UpdateProfileMedia : IAuthRequiredRequest, IRequestWithFile
{
    public string Path => "/api/user/profile-media";
    public Method Method => Method.Put;
    public string FileName { get; set; }
    public byte[] FileContent { get; set; }

    public UpdateProfileMedia(string fileName, byte[] fileContent)
    {
        FileName = fileName;
        FileContent = fileContent;
    }
}
