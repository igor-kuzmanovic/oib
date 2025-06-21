using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Contracts
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
