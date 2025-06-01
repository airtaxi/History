using History.ApiService.DataTypes;
using Microsoft.IdentityModel.Tokens;
using RestSharp;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;

namespace History.ApiService.Helpers;

public static class AppleIdTokenHelper
{
    private static readonly RestClient s_client = new("https://appleid.apple.com/auth/keys");

    /// <summary>
    /// Validate Apple ID token
    /// </summary>
    /// <param name="idToken">Apple ID token</param>
    /// <returns>OAuthPayload containing user information</returns>
    public static async Task<OAuthPayload> ValidateAppleIdTokenAsync(string idToken)
    {
        try
        {
            // Get Apple's public keys
            var appleKeys = await GetApplePublicKeysAsync();

            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(idToken);

            // Get key ID from JWT header
            var keyId = jwt.Header.Kid;
            var appleKey = appleKeys.Keys.FirstOrDefault(k => k.Kid == keyId);

            if (appleKey == null)
                return null;

            // Create RSA key
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(appleKey.N),
                Exponent = Base64UrlDecode(appleKey.E)
            });

            var securityKey = new RsaSecurityKey(rsa);

            // Set token validation parameters
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "https://appleid.apple.com",
                ValidateAudience = true,
                ValidAudience = "com.airtaxi.history", // Replace with your app's Bundle ID
                ValidateLifetime = true,
                IssuerSigningKey = securityKey,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            // Validate token
            tokenHandler.ValidateToken(idToken, validationParameters, out SecurityToken validatedToken);

            // Extract user information
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            var email = jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value;
            var name = jwt.Claims.FirstOrDefault(c => c.Type == "name")?.Value;

            return new OAuthPayload()
            {
                Id = sub,
                Email = email,
                Name = name // Apple may not always provide name
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get Apple's public keys for JWT verification
    /// </summary>
    /// <returns>Apple public keys</returns>
    private static async Task<ApplePublicKeyResponse> GetApplePublicKeysAsync()
    {
        var response = await s_client.ExecuteGetAsync<ApplePublicKeyResponse>(string.Empty);
        return response.Data;
    }

    /// <summary>
    /// Base64Url decode helper method
    /// </summary>
    /// <param name="input">Base64Url encoded string</param>
    /// <returns>Decoded bytes</returns>
    private static byte[] Base64UrlDecode(string input)
    {
        var output = input;
        output = output.Replace('-', '+').Replace('_', '/');

        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new ArgumentException("Invalid Base64Url string");
        }

        return Convert.FromBase64String(output);
    }

    // Apple public key response models
    public class ApplePublicKeyResponse
    {
        public List<ApplePublicKey> Keys { get; set; }
    }

    public class ApplePublicKey
    {
        public string Kid { get; set; }
        public string Kty { get; set; }
        public string Use { get; set; }
        public string Alg { get; set; }
        public string N { get; set; }
        public string E { get; set; }
    }

}
