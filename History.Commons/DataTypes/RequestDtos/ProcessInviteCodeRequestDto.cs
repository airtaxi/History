using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

public class ProcessInviteCodeRequestDto
{
    /// <summary>
    /// Optional message from the moderator, included in the result notification.
    /// </summary>
    [StringLength(500)]
    public string Message { get; set; }
}