using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos;

public class BulkChangeDiscoveryOptionRequestDto
{
    public DiscoveryOption? From { get; set; }
    public DiscoveryOption To { get; set; }
}
