using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace History.Commons.DataTypes.ResponseDtos
{
    public class OAuthLoginResponseDto()
    {
        public string AccessToken { get; set; }

        public OAuthLoginResponseDto(string accessToken) : this() => AccessToken = accessToken;
    }
}
