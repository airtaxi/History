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

public class ModifyPost : IAuthRequiredRequest, IRequestWithForm, IRequestWithFiles
{
    public string Path => "/api/post/modify/{postId}";
    public Method Method => Method.Post;
    public object Body { get; set; }
    public Dictionary<string, byte[]> Files { get; set; }

    public ModifyPost(List<BaseContent> contents, DiscoveryOption discoveryOption, List<string> discoveryOptionSelectedUserIds = null, Dictionary<string, byte[]> files = null)
    {
        Body = new ModifyPostRequestDto
        {
            Contents = contents,
            DiscoveryOption = discoveryOption,
            DiscoveryOptionSelectedUserIds = discoveryOptionSelectedUserIds
        };
        Files = files ?? [];
    }
}
