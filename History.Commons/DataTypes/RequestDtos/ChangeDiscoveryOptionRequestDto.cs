using History.Commons.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class ChangeDiscoveryOptionRequestDto
{
    public DiscoveryOption NewDiscoveryOption { get; set; }
    public List<string> SelectedUserIds { get; set; } = new List<string>();
}
