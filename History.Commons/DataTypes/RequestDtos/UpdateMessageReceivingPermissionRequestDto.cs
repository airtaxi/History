using History.Commons.Enums;

namespace History.Commons.DataTypes.RequestDtos;

public class UpdateMessageReceivingPermissionRequestDto
{
    public AccessPermission Permission { get; set; }
}