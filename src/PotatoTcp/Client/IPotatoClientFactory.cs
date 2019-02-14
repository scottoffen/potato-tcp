using System.Net.Sockets;

namespace PotatoTcp.Client
{
    public interface IPotatoClientFactory
    {
        IPotatoClient Create(TcpClient client);
    }
}