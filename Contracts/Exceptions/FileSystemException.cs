using System.Runtime.Serialization;

namespace Contracts.Exceptions
{
    [DataContract]
    public class FileSystemException
    {
        string message;

        [DataMember]
        public string Message { get => message; set => message = value; }

        public FileSystemException(string message)
        {
            Message = message;
        }
    }
}
