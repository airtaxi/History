using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for updating user nickname
/// </summary>
public class UpdateUserNicknameRequestDto
{
    /// <summary>
    /// The new nickname for the user profile
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string Nickname { get; set; }
}