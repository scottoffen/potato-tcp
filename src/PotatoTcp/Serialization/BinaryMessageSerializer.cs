using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PotatoTcp.Serialization
{
    public class BinaryMessageSerializer : IMessageSerializer
    {
        private readonly BinaryFormatter _serializer;

        public BinaryMessageSerializer()
        {
            _serializer = new BinaryFormatter();
        }

        public void Serialize<T>(Stream stream, T obj)
        {
            _serializer.Serialize(stream, obj);
        }

        public object Deserialize(Type _, Stream stream)
        {
            return _serializer.Deserialize(stream);
        }
    }
}