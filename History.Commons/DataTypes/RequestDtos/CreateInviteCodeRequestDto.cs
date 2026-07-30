using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

public class CreateInviteCodeRequestDto
{
    /// <summary>
    /// Optional reason for requesting invite codes.
    /// </summary>
    [MaxLength(500)]
    public string Reason { get; set; }

    /// <summary>
    /// The number of invite codes to request (1-50).
    /// </summary>
    [Range(1, 50)]
    public int Count { get; set; } = 1;
}