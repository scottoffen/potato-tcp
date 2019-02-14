using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PotatoTcp.Serialization
{
    public class BinaryEnvelopeSerializer : IEnvelopeSerializer
    {
        protected readonly BinaryFormatter Serializer = new BinaryFormatter();

        public Envelope Deserialize(Stream stream)
        {
            return (Envelope)Serializer.Deserialize(stream);
        }

        public void Serialize(Stream stream, Envelope envelope)
        {
            Serializer.Serialize(stream, envelope);
        }
    }
}