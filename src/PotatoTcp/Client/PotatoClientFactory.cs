using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PotatoTcp.Serialization;
using System.Net.Sockets;

namespace PotatoTcp.Client
{
    public class PotatoClientFactory : IPotatoClientFactory
    {
        public static readonly ILogger<PotatoClient> DefaultLogger = new NullLoggerFactory().CreateLogger<PotatoClient>();

        protected IEnvelopeSerializer EnvelopeSerializer;
        protected IMessageSerializer MessageSerializer;
        protected ILogger<PotatoClient> Logger;

        public PotatoClientFactory() : this(DefaultLogger) { }

        public PotatoClientFactory(ILogger<PotatoClient> logger) : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), logger) { }

        public PotatoClientFactory(IEnvelopeSerializer envelopeSerializer) : this(envelopeSerializer, new BinaryMessageSerializer(), DefaultLogger) { }

        public PotatoClientFactory(IMessageSerializer messageSerializer) : this(new BinaryEnvelopeSerializer(), messageSerializer, DefaultLogger) { }

        public PotatoClientFactory(IEnvelopeSerializer envelopeSerializer, ILogger<PotatoClient> logger) : this(envelopeSerializer, new BinaryMessageSerializer(), logger) { }

        public PotatoClientFactory(IMessageSerializer messageSerializer, ILogger<PotatoClient> logger) : this(new BinaryEnvelopeSerializer(), messageSerializer, logger) { }

        public PotatoClientFactory(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer) : this(envelopeSerializer, messageSerializer, DefaultLogger) { }

        public PotatoClientFactory(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer, ILogger<PotatoClient> logger)
        {
            EnvelopeSerializer = envelopeSerializer;
            MessageSerializer = messageSerializer;
            Logger = logger;
        }

        public IPotatoClient Create(TcpClient client)
            => new PotatoClient(EnvelopeSerializer, MessageSerializer, Logger, client);
    }
}