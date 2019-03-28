using System.Net.Sockets;
using PotatoTcp.HandlerStrategies;

namespace PotatoTcp.Client
{
    public interface IPotatoClientFactory
    {
        IPotatoClient Create(TcpClient client);
        IPotatoClient Create(TcpClient client, IHandlerStrategy handlerStrategy);
    }
}