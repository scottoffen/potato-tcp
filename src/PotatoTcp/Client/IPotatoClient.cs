using Microsoft.Extensions.Logging;
using PotatoTcp.Serialization;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PotatoTcp.Client
{
    /// <summary>
    /// Represents a client connection on a TCP network.
    /// </summary>
    public interface IPotatoClient : IDisposable
    {
        /// <summary>
        /// Gets or sets a value indicating whether keep alive should be enabled on the connection.
        /// </summary>
        bool EnableKeepAlive { get; set; }

        /// <summary>
        /// The serializer used to serialze and deserialize an <see cref=" Enveleope"/>.
        /// </summary>
        IEnvelopeSerializer EnvelopeSerializer { get; }

        /// <summary>
        /// Gets or sets a value indicating the host name to connect the client to.
        /// </summary>
        string HostName { get; set; }

        /// <summary>
        /// Get a value representing a unique identifer for the client.
        /// </summary>
        Guid Id { get; }

        /// <summary>
        /// Gets a value indicating whether the underlying Socket for a TcpClient is connected to a remote host.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// The number of seconds of idle time between sending keepalive packets to the remote host.
        /// </summary>
        int KeepAliveInterval { get; set; }

        /// <summary>
        /// Gets the local endpoint.
        /// </summary>
        EndPoint LocalEndPoint { get; }

        /// <summary>
        /// Gets the logger used by the client.
        /// </summary>
        ILogger<PotatoClient> Logger { get; }

        /// <summary>
        /// The serializer used to serialze and deserialize the message in <see cref=" Enveleope.Data"/>.
        /// </summary>
        IMessageSerializer MessageSerializer { get; }

        /// <summary>
        /// Occurs when the client establishes a connection with the remote host.
        /// </summary>
        event ClientConnectionEvent OnConnect;

        /// <summary>
        /// Occurs when the client connection with the remote host is disconnected.
        /// </summary>
        event ClientConnectionEvent OnDisconnect;

        /// <summary>
        /// Gets or sets the port number of the remote host to connect to.
        /// </summary>
        int Port { get; set; }

        /// <summary>
        /// Gets the remote endpoint.
        /// </summary>
        EndPoint RemoteEndPoint { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        void AddHandler<T>(Action<Guid, T> handler);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handler"></param>
        void AddHandler(IMessageHandler handler);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task ConnectAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task ConnectAsync(CancellationToken token);

        /// <summary>
        /// 
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task ListenAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task ListenAsync(CancellationToken token);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void RemoveHandler<T>();

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        void Send<T>(T obj);

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SendAsync<T>(T obj);
    }
}