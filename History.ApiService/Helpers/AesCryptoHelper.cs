using System.Security.Cryptography;
using System.Text;

namespace History.ApiService.Helpers;

public static class AesCryptoHelper
{
    public static string DecryptBase64(string cipherTextBase64, byte[] key, byte[] iv)
    {
        if (string.IsNullOrWhiteSpace(cipherTextBase64)) return null;
        var data = Convert.FromBase64String(cipherTextBase64);

        byte[] actualIv;
        byte[] cipherBytes;

        if ((iv == null || iv.Length == 0) && data.Length > 16)
        {
            actualIv = data.Take(16).ToArray();
            cipherBytes = data.Skip(16).ToArray();
        }
        else
        {
            actualIv = iv;
            cipherBytes = data;
        }

        if (actualIv == null || actualIv.Length != 16)
            throw new InvalidOperationException("AES IV must be 16 bytes or included in ciphertext.");

        using var aes = Aes.Create();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = key;
        aes.IV = actualIv;

        using var decryptor = aes.CreateDecryptor();
        var plaintextBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plaintextBytes);
    }
}
