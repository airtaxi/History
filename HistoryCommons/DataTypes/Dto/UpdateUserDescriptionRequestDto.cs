using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.Dto;

/// <summary>
/// DTO for updating user description
/// </summary>
public class UpdateUserDescriptionRequestDto
{
    /// <summary>
    /// The new description for the user profile
    /// </summary>
    [MaxLength(40)]
    public string Description { get; set; }
}