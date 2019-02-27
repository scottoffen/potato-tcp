using System;
using System.IO;

namespace PotatoTcp.Serialization
{
    public interface IWireProtocol
    {
        void Serialize<T>(Stream stream, T obj);

        object Deserialize(Stream stream);
    }
}