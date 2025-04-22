using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.MobileClient.Auth;

public interface IGoogleAuthService
{
    Task<string> AuthenticateAsync();
    Task<bool> SignOutAsync();
}
