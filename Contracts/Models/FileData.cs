using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class FileData
    {
        [DataMember]
        public byte[] Content { get; set; }

        [DataMember]
        public byte[] InitializationVector { get; set; }
    }
}
