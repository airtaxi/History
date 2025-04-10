using History.Commons.DataTypes.Contents;
using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class ModifyPostRequestDto
{
    public DiscoveryOption DiscoveryOption { get; set; }
    public List<BaseContent> Contents { get; set; } = [];
    public List<string> DiscoveryOptionSelectedUserIds { get; set; } = [];
}
