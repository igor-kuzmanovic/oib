using System.Runtime.Serialization;

namespace Contracts.Exceptions
{
    [DataContract]
    public class FileSecurityException
    {
        string message;

        [DataMember]
        public string Message { get => message; set => message = value; }

        public FileSecurityException(string message)
        {
            Message = message;
        }
    }
}
