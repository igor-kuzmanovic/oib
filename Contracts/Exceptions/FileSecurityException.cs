using System.Runtime.Serialization;

namespace Contracts.Exceptions
{
    [DataContract]
    public class FileSecurityException
    {
        [DataMember]
        public string Message { get; set; }

        public FileSecurityException(string message)
        {
            Message = message;
        }
    }
}
