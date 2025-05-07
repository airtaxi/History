using History.Commons.DataTypes.RequestDtos;
using History.Commons.DataTypes.ResponseDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class ChangeDiscoveryOption : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithBody, IRequestWithUrlParameters
{
    public string Path => "/api/post/{postId}/discovery-option";
    public Method Method => Method.Put;
    public Dictionary<string, string> UrlParameters { get; set; } = [];
    public object Body { get; set; }

    public ChangeDiscoveryOption(string postId, DiscoveryOption newDiscoveryOption , List<string> selectedUserIds = null)
    {
        UrlParameters["postId"] = postId;
        Body = new ChangeDiscoveryOptionRequestDto
        {
            NewDiscoveryOption = newDiscoveryOption,
            SelectedUserIds = selectedUserIds
        };
    }
}
