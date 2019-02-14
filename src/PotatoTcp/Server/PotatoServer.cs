using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PotatoTcp.Client;
using PotatoTcp.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PotatoTcp.Server
{
    public class PotatoServer : IPotatoServer
    {
        public static readonly ILogger<PotatoServer> DefaultLogger = new NullLoggerFactory().CreateLogger<PotatoServer>();

        public static IPAddress DefaultIpAddress { get; set; } = IPAddress.Any;
        public static int DefaultPortNumber { get; set; } = 23000;

        protected bool Starting { get; set; }
        protected bool Stopping { get; set; }

        public ILogger<PotatoServer> Logger { get; protected set; }
        protected ExtendedTcpListener TcpListener;
        protected bool Disposed { get; private set; }
        protected IDictionary<Guid, IPotatoClient> Clients = new ConcurrentDictionary<Guid, IPotatoClient>();
        protected IDictionary<Type, IMessageHandler> Handlers { get; } = new ConcurrentDictionary<Type, IMessageHandler>();
        protected IPotatoClientFactory ClientFactory { get; set; }
        public bool IsListening => TcpListener != null && TcpListener.IsActive;

        public IEnvelopeSerializer EnvelopeSerializer { get; protected set; }

        public IMessageSerializer MessageSerializer { get; protected set; }

        public IPEndPoint IpEndpoint { get; set; } = new IPEndPoint(DefaultIpAddress, DefaultPortNumber);

        public event ClientConnectionEvent OnClientConnect;
        public event ServerListeningEvent OnStart;
        public event ServerListeningEvent OnStop;

        public PotatoServer() : this(DefaultLogger) { }

        public PotatoServer(ILogger<PotatoServer> logger) : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), logger, new PotatoClientFactory()) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer) : this(envelopeSerializer, new BinaryMessageSerializer(), DefaultLogger, new PotatoClientFactory(envelopeSerializer)) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, ILogger<PotatoServer> logger) : this(envelopeSerializer, new BinaryMessageSerializer(), logger, new PotatoClientFactory(envelopeSerializer)) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer) : this(envelopeSerializer, messageSerializer, DefaultLogger, new PotatoClientFactory(envelopeSerializer, messageSerializer)) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, IPotatoClientFactory factory) : this(envelopeSerializer, new BinaryMessageSerializer(), DefaultLogger, factory) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer, ILogger<PotatoServer> logger) : this(envelopeSerializer, messageSerializer, logger, new PotatoClientFactory(envelopeSerializer, messageSerializer)) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, IPotatoClientFactory factory, ILogger<PotatoServer> logger) : this(envelopeSerializer, new BinaryMessageSerializer(), logger, factory) { }

        public PotatoServer(IMessageSerializer messageSerializer) : this(new BinaryEnvelopeSerializer(), messageSerializer, DefaultLogger, new PotatoClientFactory(messageSerializer)) { }

        public PotatoServer(IMessageSerializer messageSerializer, ILogger<PotatoServer> logger) : this(new BinaryEnvelopeSerializer(), messageSerializer, logger, new PotatoClientFactory(messageSerializer)) { }

        public PotatoServer(IMessageSerializer messageSerializer, IPotatoClientFactory factory) : this(new BinaryEnvelopeSerializer(), messageSerializer, DefaultLogger, factory) { }

        public PotatoServer(IMessageSerializer messageSerializer, IPotatoClientFactory factory, ILogger<PotatoServer> logger) : this(new BinaryEnvelopeSerializer(), messageSerializer, logger, factory) { }

        public PotatoServer(IPotatoClientFactory factory) : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), DefaultLogger, factory) { }

        public PotatoServer(IPotatoClientFactory factory, ILogger<PotatoServer> logger) : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), logger, factory) { }

        public PotatoServer(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer, ILogger<PotatoServer> logger, IPotatoClientFactory clientFactory)
        {
            EnvelopeSerializer = envelopeSerializer;
            MessageSerializer = messageSerializer;
            Logger = logger;
            ClientFactory = clientFactory;

            Logger.LogTrace("PotatoTcp Server initialized.");
        }

        public void Send<T>(T message)
        {
            foreach (var client in Clients.Values)
            {
                client.Send(message);
            }
        }

        public Task SendAsync<T>(T message)
        {
            return Task.WhenAll(Clients.Values.Select(client => client.SendAsync(message)));
        }

        public void Send<T>(Guid clientId, T message)
        {
            if (!Clients.ContainsKey(clientId)) return;
            Clients[clientId].Send(message);
        }

        public async Task SendAsync<T>(Guid clientId, T message)
        {
            if (!Clients.ContainsKey(clientId)) return;
            await Clients[clientId].SendAsync(message);
        }

        public async Task StartAsync()
        {
            await StartAsync(CancellationToken.None);
        }

        public async Task StartAsync(CancellationToken token)
        {
            if (IsListening || Starting || Stopping) return;

            try
            {
                Starting = true;
                TcpListener = new ExtendedTcpListener(IpEndpoint);
                TcpListener.Start();
                Starting = false;

                OnStart?.Invoke(this);

                while (true)
                {
                    var client = ClientFactory.Create(await TcpListener.AcceptTcpClientAsync());
                    Clients.Add(client.Id, client);
                    OnClientConnect?.Invoke(client);

                    foreach (var handler in Handlers.Values)
                    {
                        client.AddHandler(handler);
                    }

                    client.ListenAsync(token);
                }
            }
            catch (SocketException se) when (Stopping)
            {
                /* Thrown by:
                   - TcpListener.Start()
                   - TcpListener.AcceptTcpClientAsync()

                    Use the ErrorCode property to obtain the specific error code, then refer to:
                    https://docs.microsoft.com/en-us/windows/desktop/winsock/windows-sockets-error-codes-2
                 */
                Logger.LogInformation(se, $"Ignoring exception because listener is in shutdown mode.");
            }
            catch (ObjectDisposedException ode) when (Stopping)
            {
                Logger.LogInformation(ode, $"Ignoring exception because listener is in shutdown mode.");
            }
            catch (ArgumentNullException)
            {
                throw;
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
            finally
            {
                Starting = false;
            }
        }

        public void Stop()
        {
            if (!IsListening || Starting || Stopping) return;
            Stopping = true;

            try
            {
                TcpListener.Stop();
                TcpListener = null;

                Parallel.ForEach(Clients.Values, client => client.Disconnect());
                Clients.Clear();

                OnStop?.Invoke(this);
            }
            catch (Exception e)
            {
                Logger.LogDebug(e, e.Message);
            }
            finally
            {
                Stopping = false;
            }
        }

        public void AddHandler<T>(Action<Guid, T> handler) where T : class
        {
            foreach (var client in Clients.Values)
            {
                client.AddHandler<T>(handler);
            }

            var handlerType = typeof(T);
            Handlers.Add(handlerType, new MessageHandler<T>
            {
                HandlerType = handlerType,
                HandlerAction = handler
            });
        }

        public void RemoveHandler<T>()
        {
            Handlers.Remove(typeof(T));
            Clients.ToList().ForEach(c => c.Value.RemoveHandler<T>());
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            Stop();
        }
    }
}