using System.Runtime.Serialization;

[DataContract]
public class FileData
{
    [DataMember]
    public byte[] Content { get; set; }

    [DataMember]
    public byte[] InitializationVector { get; set; }
}