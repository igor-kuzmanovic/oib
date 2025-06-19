using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Contracts;

namespace Server
{
    public static class EncryptionHelper
    {
        private const int KEY_SIZE = 256;
        private const int BLOCK_SIZE = 128;

        public static byte[] GetSecretKey()
        {
            string keyString = EncryptionConfigFile.EncryptionKey;
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