using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using Contracts;

namespace Server
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
                    Console.WriteLine("Using EncryptionKey from App.config");
                    return key;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading EncryptionKey from App.config: {ex.Message}");
            }

            Console.WriteLine("Using default EncryptionKey");
            return defaultKey;
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