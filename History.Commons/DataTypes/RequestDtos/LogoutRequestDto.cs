using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for logging out a user session
/// </summary>
public class LogoutRequestDto
{
    /// <summary>
    /// The refresh token to revoke on logout
    /// </summary>
    [Required]
    public string RefreshToken { get; set; }
}
