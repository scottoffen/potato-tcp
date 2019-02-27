using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PotatoTcp.Serialization;
using System.Net.Sockets;

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
    }
}