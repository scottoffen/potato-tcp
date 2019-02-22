using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PotatoTcp.Serialization
{
    public class BinaryFormatterWireProtocol : IWireProtocol
    {
        private readonly BinaryFormatter _serializer = new BinaryFormatter();

        public void Serialize<T>(Stream stream, T obj)
        {
            _serializer.Serialize(stream, obj);
        }

        public object Deserialize(Stream stream)
        {
            return _serializer.Deserialize(stream);
        }
    }
}