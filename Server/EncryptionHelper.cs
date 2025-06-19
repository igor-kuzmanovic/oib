using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Server
{
    public static class EncryptionHelper
    {
        private static readonly string KeyFileName = "aes_key.bin";
        private static readonly byte[] DefaultKey = Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // 32 bytes = 256-bit key

        public static byte[] GetSecretKey()
        {
            string keyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, KeyFileName);

            // If key file doesn't exist, generate and save a new key
            if (!File.Exists(keyPath))
            {
                try
                {
                    using (Aes aes = Aes.Create())
                    {
                        aes.KeySize = 256;
                        aes.GenerateKey();
                        File.WriteAllBytes(keyPath, aes.Key);
                        return aes.Key;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error generating key: {ex.Message}. Using default key.");
                    return DefaultKey;
                }
            }

            try
            {
                // Read existing key
                return File.ReadAllBytes(keyPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading key: {ex.Message}. Using default key.");
                return DefaultKey;
            }
        }
    }
}