using System.Runtime.Serialization;

namespace Contracts.Exceptions
{
    [DataContract]
    public class FileSystemException
    {
        [DataMember]
        public string Message { get; set; }

        public FileSystemException(string message)
        {
            Message = message;
        }
    }
}
