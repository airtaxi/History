using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

public class CreateInviteCodeByAdminRequestDto
{
    /// <summary>
    /// The user ID to assign the invite codes to.
    /// </summary>
    [Required]
    public string OwnerId { get; set; }

    /// <summary>
    /// The number of invite codes to create (1-100).
    /// </summary>
    [Range(1, 100)]
    public int Count { get; set; } = 1;
}