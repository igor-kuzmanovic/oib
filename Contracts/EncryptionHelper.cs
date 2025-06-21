using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;

namespace Contracts
{
    public static class EncryptionHelper
    {
        private const int KEY_SIZE = 256;
        private const int BLOCK_SIZE = 128;

        public static byte[] GetSecretKey()
        {
            string keyString = GetEncryptionKey();
            byte[] key = Encoding.UTF8.GetBytes(keyString);

            if (key.Length != KEY_SIZE / 8)
            {
                using (SHA256 sha = SHA256.Create())
                {
                    key = sha.ComputeHash(key);
                }
            }

            Console.WriteLine("Encryption key length: " + key.Length);
            return key;
        }

        private static string GetEncryptionKey()
        {
            string defaultKey = "12345678901234567890123456789012";
            try
            {
                string key = ConfigurationManager.AppSettings["EncryptionKey"];
                if (!string.IsNullOrEmpty(key))
                {
                    Console.WriteLine("Using EncryptionKey from App.config: " + GetKeyPreview(key));
                    return key;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading EncryptionKey from App.config: {ex.Message}");
            }

            Console.WriteLine("Using default EncryptionKey: " + GetKeyPreview(defaultKey));
            return defaultKey;
        }

        // Helper for safe preview
        private static string GetKeyPreview(string key)
        {
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 8) + "...";
            }
        }

        public static FileData EncryptContent(byte[] content)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = GetSecretKey();
                aes.BlockSize = BLOCK_SIZE;
                aes.KeySize = KEY_SIZE;
                aes.GenerateIV();
                Console.WriteLine("Encrypting content. IV: " + BitConverter.ToString(aes.IV));

                byte[] encryptedContent;

                using (MemoryStream msEncrypt = new MemoryStream())
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    csEncrypt.Write(content, 0, content.Length);
                    csEncrypt.FlushFinalBlock();
                    encryptedContent = msEncrypt.ToArray();
                }

                return new FileData
                {
                    InitializationVector = aes.IV,
                    Content = encryptedContent
                };
            }
        }

        public static byte[] DecryptContent(FileData fileData)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                aes.Key = GetSecretKey();
                aes.IV = fileData.InitializationVector;
                aes.BlockSize = BLOCK_SIZE;
                aes.KeySize = KEY_SIZE;

                Console.WriteLine("Decrypting content. IV: " + BitConverter.ToString(fileData.InitializationVector));
                Console.WriteLine("FileData.Content length: " + fileData.Content?.Length);
                Console.WriteLine("FileData.InitializationVector length: " + fileData.InitializationVector?.Length);

                using (MemoryStream msDecrypt = new MemoryStream())
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    csDecrypt.Write(fileData.Content, 0, fileData.Content.Length);
                    csDecrypt.FlushFinalBlock();
                    return msDecrypt.ToArray();
                }
            }
        }
    }
}