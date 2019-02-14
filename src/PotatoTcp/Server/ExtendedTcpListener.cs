using System.Net;
using System.Net.Sockets;

namespace PotatoTcp.Server
{
    /// <summary>
    /// Class that extends System.Net.Sockets.TcpListener to expose the protected property Active as the public property IsActive
    /// </summary>
    public class ExtendedTcpListener : TcpListener
    {
        /// <summary>
        /// Gets a value that indicates whether TcpListener is actively listening for client connections.
        /// </summary>
        public bool IsActive => Active;

        /// <inheritdoc />
        public ExtendedTcpListener(IPEndPoint ipEndPoint) : base(ipEndPoint) { }

        /// <inheritdoc />
        public ExtendedTcpListener(IPAddress iPAddress, int port) : base(iPAddress, port) { }
    }
}