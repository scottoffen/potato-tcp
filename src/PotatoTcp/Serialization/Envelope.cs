using System;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PotatoTcp
{
    [Serializable]
    public class Envelope : IEnvelope
    {
        public Guid ClientId { get; set; }
        public MessageType MessageType { get; set; }
        public string DataType { get; set; }
        public Stream Data { get; set; }

        public Envelope()
        {
        }

        public Envelope(SerializationInfo info, StreamingContext context)
        {
            ClientId = Guid.Parse(info.GetString(nameof(ClientId)));
            MessageType = (MessageType) info.GetInt32(nameof(MessageType));
            DataType = info.GetString(nameof(DataType));
            Data = new MemoryStream((byte[]) info.GetValue(nameof(Data), typeof(byte[])));
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(ClientId), ClientId);
            info.AddValue(nameof(MessageType), (int) MessageType);
            info.AddValue(nameof(DataType), DataType);
            info.AddValue(nameof(Data), ((MemoryStream) Data).ToArray());
        }
    }

    public enum MessageType
    {
        Ping,
        Pong,
        Content
    }
}