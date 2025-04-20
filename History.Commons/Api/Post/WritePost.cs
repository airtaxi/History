using History.Commons.DataTypes.Contents;
using History.Commons.DataTypes.RequestDtos;
using History.Commons.Enums;
using History.Commons.Interfaces;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.Api.Post;

public class WritePost : IAuthRequiredRequest, IRequestWithBody, IRequestWithFiles
{
    public string Path => "/api/post/ignore/{postId}";
    public Method Method => Method.Post;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public WritePost(List<BaseContent> contents, DiscoveryOption discoveryOption, string ParentPostId = null, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null)
    {
        Body = new WritePostRequestDto
        {
            Contents = contents,
            DiscoveryOption = discoveryOption,
            ParentPostId = ParentPostId,
            DiscoveryOptionSelectedUserIds = discoveryOptionSelectedUserIds
        };
        Files = files ?? [];
    }
}
