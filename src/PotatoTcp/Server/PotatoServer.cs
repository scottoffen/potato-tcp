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
using PotatoTcp.HandlerStrategies;

namespace PotatoTcp.Server
{
    public class PotatoServer : IPotatoServer
    {
        public static readonly ILogger<PotatoServer> DefaultLogger = new NullLoggerFactory().CreateLogger<PotatoServer>();

        public static IPAddress DefaultIpAddress { get; set; } = IPAddress.Any;

        public static int DefaultPortNumber { get; set; } = 23000;

        protected ExtendedTcpListener TcpListener { get; set; }

        public IPotatoClientFactory ClientFactory { get; protected set; }

        public bool Disposed { get; private set; }

        public bool Starting { get; protected set; }

        public bool Stopping { get; protected set; }

        public readonly IDictionary<Guid, IPotatoClient> Clients = new ConcurrentDictionary<Guid, IPotatoClient>();

        public bool IsListening => TcpListener?.IsActive ?? false;

        public IPEndPoint IpEndpoint { get; set; } = new IPEndPoint(DefaultIpAddress, DefaultPortNumber);

        public ILogger<PotatoServer> Logger { get; protected set; }

        public event ClientConnectionEvent OnClientConnect;
        public event ServerListeningEvent OnStart;
        public event ServerListeningEvent OnStop;

        private readonly IHandlerStrategy _messageHandlerStrategy;

        public PotatoServer() : this(null, new PotatoClientFactory(), DefaultLogger, new DerivedTypeHandlerStrategy()) { }

        public PotatoServer(IWireProtocol wireProtocol) : this(wireProtocol, new PotatoClientFactory(wireProtocol), DefaultLogger, new DerivedTypeHandlerStrategy()) { }

        public PotatoServer(IPotatoClientFactory factory) : this(null, factory, DefaultLogger, new DerivedTypeHandlerStrategy()) { }

        public PotatoServer(ILogger<PotatoServer> logger) : this(null, new PotatoClientFactory(), logger, new DerivedTypeHandlerStrategy()) { }

        public PotatoServer(IWireProtocol wireProtocol, ILogger<PotatoServer> logger) : this(wireProtocol, new PotatoClientFactory(wireProtocol), logger, new DerivedTypeHandlerStrategy()) { }

        public PotatoServer(IWireProtocol protocol, IPotatoClientFactory factory, ILogger<PotatoServer> logger, IHandlerStrategy handlerStrategy)
        {
            ClientFactory = factory;
            Logger = logger;
            _messageHandlerStrategy = handlerStrategy;

            Logger.LogTrace("PotatoTcp Server initialized.");
        }

        public virtual void AddHandler<T>(Action<Guid, T> handler) where T : class
        {
            _messageHandlerStrategy.AddHandler(handler);
        }

        public virtual void Disconnect(Guid Id)
        {
            Clients[Id].Disconnect();
            Clients.Remove(Id);
        }

        public virtual void Disconnect()
        {
            Parallel.ForEach(Clients.Values, client => client.Disconnect());
            Clients.Clear();
        }

        public void Dispose()
        {
            if (Disposed) return;
            Stop();
            Disconnect();
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        public virtual bool TryRemoveHandler<T>()
        {
            return _messageHandlerStrategy.TryRemoveHandlers<T>();
        }

        public virtual void Send<T>(T message) where T : class
        {
            foreach (var client in Clients.Values) client.Send(message);
        }

        public virtual void Send<T>(Guid clientId, T message) where T : class
        {
            Clients[clientId]?.Send(message);
        }

        public virtual Task SendAsync<T>(T message) where T : class
        {
            return Task.WhenAll(Clients.Values.Select(client => client.SendAsync(message)));
        }

        public virtual async Task SendAsync<T>(Guid clientId, T message) where T : class
        {
            await Clients[clientId]?.SendAsync(message);
        }

        public virtual async Task StartAsync()
        {
            await StartAsync(CancellationToken.None);
        }

        public virtual async Task StartAsync(CancellationToken token)
        {
            if (IsListening || Starting || Stopping) return;

            try
            {
                Starting = true;
                TcpListener = new ExtendedTcpListener(IpEndpoint);
                TcpListener.Start();
                Starting = false;

                OnStart?.Invoke(this);

                while (!Stopping)
                {
                    var client = ClientFactory.Create(await TcpListener.AcceptTcpClientAsync(), _messageHandlerStrategy);

                    Clients.Add(client.Id, client);
                    OnClientConnect?.Invoke(client);

                    client.ListenAsync(token);
                }
            }
            catch (SocketException se) when (Stopping)
            {
                Logger.LogTrace(se, $"Ignoring exception because listener is in shutdown mode.");
            }
            catch (SocketException se)
            {
                Logger.LogTrace(se, $"SocketException.ErrorCode: {se.ErrorCode}");
                throw;
            }
            catch (ObjectDisposedException ode) when (Stopping || TcpListener == null)
            {
                Logger.LogTrace(ode, $"Ignoring exception because listener is in shutdown mode.");
            }
            catch (ArgumentNullException) when (Starting)
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

        public virtual void Stop()
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Stop on a disposed object.");
            if (!IsListening || Starting || Stopping) return;

            try
            {
                Stopping = true;

                TcpListener.Stop();
                TcpListener = null;

                Stopping = false;

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
    }
}