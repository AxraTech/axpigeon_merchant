using System.Security.Cryptography;
using System.Text;

namespace AxpigeonApp.Utils
{
    public static class EncryptUtil
    {
        public static string EncryptMessage(string content, string secretKey)
        {
            byte[] keyBytes = FormatKey(secretKey); // 32 bytes
            byte[] ivBytes = FormatIV(secretKey);  // 16 bytes

            byte[] plainBytes = Encoding.UTF8.GetBytes(content);

            using var aes = Aes.Create();
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7; // PKCS5 == PKCS7
            aes.KeySize = 256;
            aes.Key = keyBytes;
            aes.IV = ivBytes;

            using var encryptor = aes.CreateEncryptor();
            byte[] encrypted = encryptor.TransformFinalBlock(
                plainBytes, 0, plainBytes.Length
            );

            return Convert.ToBase64String(encrypted);
        }
        // 🔑 EXACT MATCH with Java formatKey()
        private static byte[] FormatKey(string secretKey)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedKey = new byte[32]; // AES-256
            int len = Math.Min(keyBytes.Length, 32);
            Array.Copy(keyBytes, formattedKey, len);
            return formattedKey;
        }

        // 🔑 EXACT MATCH with Java formatIV()
        private static byte[] FormatIV(string secretKey)
        {
            byte[] ivBytes = Encoding.UTF8.GetBytes(secretKey);
            byte[] formattedIV = new byte[16];
            int len = Math.Min(ivBytes.Length, 16);
            Array.Copy(ivBytes, formattedIV, len);
            return formattedIV;
        }

    }
}
