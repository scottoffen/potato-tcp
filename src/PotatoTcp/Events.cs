using PotatoTcp.Client;
using PotatoTcp.Server;

namespace PotatoTcp
{
    public delegate void ClientConnectionEvent(IPotatoClient client);

    public delegate void ServerListeningEvent(IPotatoServer server);
}