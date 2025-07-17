using System;
using System.Runtime.Serialization;

namespace Contracts.Models
{
    public enum StorageEventType
    {
        CreateFile,
        CreateFolder,
        Delete,
        Move,
        Rename
    }

    [DataContract]
    public class StorageEvent
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public StorageEventType EventType { get; set; }

        [DataMember]
        public string SourcePath { get; set; }

        [DataMember]
        public string DestinationPath { get; set; }

        [DataMember]
        public byte[] Content { get; set; }

        [DataMember]
        public DateTime Timestamp { get; set; }
    }
}
