using History.Commons.DataTypes.Contents;
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

public class ModifyPost : IBaseRequest<PostResponseDto>, IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles, IRequestWithUrlParameters
{
    public string Path => "/api/post/modify/{postId}";
    public Method Method => Method.Post;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }
    public Dictionary<string, string> UrlParameters { get; set; } = [];

    public ModifyPost(string postId, List<BaseContent> contents, DiscoveryOption discoveryOption, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null)
    {
        Body = new ModifyPostRequestDto
        {
            Contents = contents,
            DiscoveryOption = discoveryOption,
            DiscoveryOptionSelectedUserIds = discoveryOptionSelectedUserIds
        };
        Files = files ?? [];
        UrlParameters["postId"] = postId;
    }
}
