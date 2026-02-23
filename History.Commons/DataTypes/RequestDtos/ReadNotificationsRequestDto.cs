using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

public class ReadNotificationsRequestDto
{
    [Required]
    public List<string> NotificationIds { get; set; }
}
