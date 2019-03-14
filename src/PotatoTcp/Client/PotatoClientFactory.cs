using Microsoft.Extensions.Logging;
using PotatoTcp.Serialization;
using System.Net.Sockets;
using PotatoTcp.HandlerStrategies;

namespace PotatoTcp.Client
{
    public class PotatoClientFactory : IPotatoClientFactory
    {
        public ILogger<PotatoClient> Logger { get; protected set; }

        public IWireProtocol WireProtocol { get; protected set; }

        public PotatoClientFactory() : this(new BinaryFormatterWireProtocol(), PotatoClient.DefaultLogger) { }

        public PotatoClientFactory(IWireProtocol wireProtocol) : this(wireProtocol, PotatoClient.DefaultLogger) { }

        public PotatoClientFactory(ILogger<PotatoClient> logger) : this(new BinaryFormatterWireProtocol(), logger) { }

        public PotatoClientFactory(IWireProtocol wireProtocol, ILogger<PotatoClient> logger)
        {
            WireProtocol = wireProtocol;
            Logger = logger;
        }

        public IPotatoClient Create(TcpClient client)
            => new PotatoClient(WireProtocol, Logger, client);

        public IPotatoClient Create(TcpClient client, IHandlerStrategy handlerStrategy)
            => new PotatoClient(WireProtocol, Logger, client, handlerStrategy);

    }
}