using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for updating user description
/// </summary>
public class UpdateUserDescriptionRequestDto
{
    /// <summary>
    /// The new description for the user profile
    /// </summary>
    [MaxLength(CommonsConstants.MaxDescriptionLength)]
    public string Description { get; set; }
}