using History.MobileClient.DataTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Social.Interfaces;

public interface IGoogleAuthService
{
    public Task<GoogleUserDTO> AuthenticateAsync();
    public Task LogoutAsync();
    public Task<GoogleUserDTO> GetCurrentUserAsync();
}
