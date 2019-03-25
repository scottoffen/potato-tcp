using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PotatoTcp.Client;

namespace PotatoTcp.Serialization
{
    public class LengthPrefixedWireProtocol : IWireProtocol
    {
        private readonly BinaryFormatter _serializer = new BinaryFormatter();

        public void Serialize<T>(Stream stream, T obj)
        {
            using (var dataStream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true))
            {
                _serializer.Serialize(dataStream, obj);

                var bytes = dataStream.ToArray();

                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }

        public object Deserialize(Stream stream)
        {
            var messageLength = ReadMessageLength(stream);

            if (messageLength == 0)
                throw new NotConnectedException("Received message of length 0.");

            using (var dataStream = new MemoryStream(messageLength))
            {
                CopyStreamAsync(stream, dataStream, messageLength).GetAwaiter().GetResult();
                dataStream.Seek(0, SeekOrigin.Begin);
                return _serializer.Deserialize(dataStream);
            }
        }

        private int ReadMessageLength(Stream stream)
        {
            var bytes = new byte[sizeof(int)];
            return (stream.Read(bytes, 0, sizeof(int)) > 0) ? BitConverter.ToInt32(bytes, 0) : 0;
        }

        private async Task CopyStreamAsync(Stream sourceStream, Stream destStream, int size)
        {
            int sizeRead = 0;
            int bufferSize = Math.Min(size, 4096);
            var buffer = new byte[bufferSize];
            int count;

            async Task<int> ReadFromSource(int sizeToRead)
            {
                var token = (new CancellationTokenSource(TimeSpan.FromSeconds(10))).Token;

                return await sourceStream.ReadAsync(buffer, 0, sizeToRead, token);
            }

            while (sizeRead < size && ((count = await ReadFromSource(Math.Min(bufferSize, size - sizeRead))) > 0))
            {
                destStream.Write(buffer, 0, count);
                sizeRead += count;
            }
        }
    }
}