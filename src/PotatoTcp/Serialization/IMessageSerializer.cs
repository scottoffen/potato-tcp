using System;
using System.IO;

namespace PotatoTcp.Serialization
{
    public interface IMessageSerializer
    {
        void Serialize<T>(Stream stream, T obj);

        object Deserialize(Type type, Stream stream);
    }
}