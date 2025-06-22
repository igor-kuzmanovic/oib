using System.Runtime.Serialization;

namespace Contracts.Exceptions
{
    [DataContract]
    public class SecurityException
    {
        string message;

        [DataMember]
        public string Message { get => message; set => message = value; }

        public SecurityException(string message)
        {
            Message = message;
        }
    }
}
