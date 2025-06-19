using System.Text;

namespace Server
{
    public static class EncryptionHelper
    {
        public static byte[] GetSecretKey()
        {
            // 32 bytes = 256-bit key
            return Encoding.UTF8.GetBytes("12345678901234567890123456789012"); // TODO Load from file?
        }
    }
}