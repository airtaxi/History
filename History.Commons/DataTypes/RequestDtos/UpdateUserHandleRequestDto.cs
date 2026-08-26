using System.ComponentModel.DataAnnotations;

namespace History.Commons.DataTypes.RequestDtos;

/// <summary>
/// DTO for updating user handle
/// </summary>
public class UpdateUserHandleRequestDto
{
    /// <summary>
    /// The new handle for the user
    /// </summary>
    [MaxLength(CommonConstants.MaxHandleLength)]
    public string Handle { get; set; }
}