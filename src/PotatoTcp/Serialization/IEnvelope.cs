using System;
using System.IO;
using System.Runtime.Serialization;

namespace PotatoTcp
{
    public interface IEnvelope : ISerializable
    {
        Guid ClientId { get; set; }
        Stream Data { get; set; }
        string DataType { get; set; }
        MessageType MessageType { get; set; }
    }
}