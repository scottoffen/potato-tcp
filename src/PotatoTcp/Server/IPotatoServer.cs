using Microsoft.Extensions.Logging;
using PotatoTcp.Serialization;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace PotatoTcp.Server
{
    /// <summary>
    /// Listens for and manages incoming TCP connections.
    /// </summary>
    public interface IPotatoServer : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// 
        /// </summary>
        IPEndPoint IpEndpoint { get; set; }

        /// <summary>
        /// 
        /// </summary>
        ILogger<PotatoServer> Logger { get; }

        /// <summary>
        /// 
        /// </summary>
        event ClientConnectionEvent OnClientConnect;

        /// <summary>
        /// 
        /// </summary>
        event ServerListeningEvent OnStart;

        /// <summary>
        /// 
        /// </summary>
        event ServerListeningEvent OnStop;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="handler"></param>
        void AddHandler<T>(Action<Guid, T> handler) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>

        void Disconnect(Guid id);

        void Disconnect();

        bool TryRemoveHandler<T>();
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        void Send<T>(T message) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync<T>(T message) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientId"></param>
        /// <param name="message"></param>
        void Send<T>(Guid clientId, T message) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clientId"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        Task SendAsync<T>(Guid clientId, T message) where T : class;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Task StartAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        Task StartAsync(CancellationToken token);

        /// <summary>
        /// 
        /// </summary>
        void Stop();
    }

    public static class IPotatoServerExtensions
    {
        public static void StopAndDisconnect(this IPotatoServer server)
        {
            server.Stop();
            server.Disconnect();
        }
    }
}