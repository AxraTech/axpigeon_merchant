using System.Security.Cryptography;
using System.Text;

namespace AxpigeonApp.Utils
{
    public static class ServerKeyUtil
    {
        private static byte[]? _masterKey;

        public static void Initialize(string masterKeyHex)
        {
            _masterKey = HexToBytes(masterKeyHex);
            if (_masterKey.Length != 32)
                throw new InvalidOperationException(
                    "SERVER_MASTER_KEY must be exactly 64 hex chars (32 bytes)");
        }

        public static string WrapDek(string plaintextDek)
        {
            EnsureInitialized();
            byte[] iv = RandomNumberGenerator.GetBytes(12);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintextDek);
            byte[] ciphertext = new byte[plainBytes.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(_masterKey!, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(iv, plainBytes, ciphertext, tag);

            byte[] output = new byte[iv.Length + ciphertext.Length + tag.Length];
            Buffer.BlockCopy(iv, 0, output, 0, iv.Length);
            Buffer.BlockCopy(ciphertext, 0, output, iv.Length, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, output, iv.Length + ciphertext.Length, tag.Length);

            return Convert.ToBase64String(output);
        }

        public static string UnwrapDek(string wrappedDekBase64)
        {
            EnsureInitialized();
            byte[] combined = Convert.FromBase64String(wrappedDekBase64);
            if (combined.Length < 12 + 1 + 16)
                throw new ArgumentException("Invalid wrapped DEK format");

            byte[] iv = new byte[12];
            byte[] tag = new byte[16];
            byte[] ciphertext = new byte[combined.Length - 12 - 16];

            Buffer.BlockCopy(combined, 0, iv, 0, 12);
            Buffer.BlockCopy(combined, 12, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(combined, combined.Length - 16, tag, 0, 16);

            byte[] plaintext = new byte[ciphertext.Length];
            using var aes = new AesGcm(_masterKey!, AesGcm.TagByteSizes.MaxSize);
            aes.Decrypt(iv, ciphertext, tag, plaintext);

            return Encoding.UTF8.GetString(plaintext);
        }

        public static string WrapDekWithPassphrase(string plaintextDek, string passphrase, out string saltHex, out string ivHex)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] iv = RandomNumberGenerator.GetBytes(12);

            byte[] kek = DeriveKek(passphrase, salt);
            byte[] plainBytes = Encoding.UTF8.GetBytes(plaintextDek);
            byte[] ciphertext = new byte[plainBytes.Length];
            byte[] tag = new byte[16];

            using var aes = new AesGcm(kek, AesGcm.TagByteSizes.MaxSize);
            aes.Encrypt(iv, plainBytes, ciphertext, tag);

            byte[] output = new byte[ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ciphertext, 0, output, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, output, ciphertext.Length, tag.Length);

            saltHex = Convert.ToHexString(salt).ToLower();
            ivHex = Convert.ToHexString(iv).ToLower();
            return Convert.ToBase64String(output);
        }

        public static byte[] DeriveKek(string passphrase, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(
                passphrase, salt, 100_000, HashAlgorithmName.SHA256);
            return pbkdf2.GetBytes(32);
        }

        private static void EnsureInitialized()
        {
            if (_masterKey == null)
                throw new InvalidOperationException(
                    "ServerKeyUtil not initialized. Call Initialize() at startup.");
        }

        private static byte[] HexToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                throw new ArgumentException("Invalid hex string for master key");

            byte[] bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return bytes;
        }
    }
}
