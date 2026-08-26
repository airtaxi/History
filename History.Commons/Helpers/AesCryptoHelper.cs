using System.Security.Cryptography;
using System.Text;

namespace History.Commons.Helpers;

public static class AesCryptoHelper
{
    public static string Encrypt(string plainText, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        using var aes = Aes.Create();
        aes.Key = keyBytes;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string key)
    {
        var keyBytes = Encoding.UTF8.GetBytes(key);
        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = keyBytes;

        var iv = new byte[aes.BlockSize / 8];
        var encryptedBytes = new byte[fullBytes.Length - iv.Length];
        Buffer.BlockCopy(fullBytes, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}
