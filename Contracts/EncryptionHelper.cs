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
            string keyString = ConfigurationManager.AppSettings["EncryptionKey"] ?? throw new InvalidOperationException("EncryptionKey is missing in App.config.");

            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(Encoding.UTF8.GetBytes(keyString));
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

                byte[] encryptedContent;

                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, aes.CreateEncryptor(aes.Key, aes.IV), CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(content, 0, content.Length);
                        csEncrypt.FlushFinalBlock();
                        encryptedContent = msEncrypt.ToArray();
                    }
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
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, aes.CreateDecryptor(aes.Key, aes.IV), CryptoStreamMode.Write))
                    {
                        csDecrypt.Write(fileData.Content, 0, fileData.Content.Length);
                        csDecrypt.FlushFinalBlock();
                        return msDecrypt.ToArray();
                    }
                }
            }
        }
    }
}