using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using Contracts.Models;

namespace Contracts.Encryption
{
    public static class EncryptionHelper
    {
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
                aes.Key = GetSecretKey();
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
                aes.Key = GetSecretKey();
                aes.IV = fileData.InitializationVector;

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
