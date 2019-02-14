using System.IO;

namespace PotatoTcp.Serialization
{
    public interface IEnvelopeSerializer
    {
        void Serialize(Stream stream, Envelope envelope);

        Envelope Deserialize(Stream stream);
    }
}