using System;
using System.Runtime.Serialization;

namespace Contracts.Models
{
    [DataContract]
    public class FileData
    {
        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public byte[] Content { get; set; }

        [DataMember]
        public byte[] InitializationVector { get; set; }

        [DataMember]
        public string CreatedBy { get; set; }

        [DataMember]
        public DateTime CreatedAt { get; set; }

        [DataMember]
        public bool IsFile { get; set; }
    }
}
