using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for updating user birthday
/// </summary>
public class UpdateUserBirthdayRequestDto
{
    /// <summary>
    /// The new birthday for the user profile. Null if user did not set or don't want to.
    /// </summary>
    public DateTime? Birthday { get; set; }
}