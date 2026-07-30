using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.RequestDtos;

public class OAuthRegisterRequestDto : OAuthLoginRequestDto
{
    public string Name { get; set; }

    /// <summary>
    /// Invite code required to register (not needed for the very first user).
    /// </summary>
    public string InviteCode { get; set; }
}
