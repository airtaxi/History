using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos;

public class BulkChangeDiscoveryOptionByPostIdsRequestDto
{
    public List<string> PostIds { get; set; }
    public DiscoveryOption To { get; set; }
}
