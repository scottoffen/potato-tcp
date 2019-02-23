using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PotatoTcp.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Timer = System.Timers.Timer;

namespace PotatoTcp.Client
{
    public class PotatoClient : IPotatoClient
    {
        public static readonly ILogger<PotatoClient> DefaultLogger = new NullLoggerFactory().CreateLogger<PotatoClient>();

        public object KeepAliveMessage { get; set; }

        private bool _enableKeepAlive;
        private readonly BlockingCollection<(TaskCompletionSource<object>, object)> _outgoingMessages = new BlockingCollection<(TaskCompletionSource<object>, object)>();

        protected readonly object SendLock = new object();

        protected TcpClient TcpClient { get; set; }

        public IDictionary<Type, IMessageHandler> Handlers { get; } = new ConcurrentDictionary<Type, IMessageHandler>();

        public bool Disposed { get; private set; }

        public TimeSpan KeepAliveTimeSpan { get; protected set; } = TimeSpan.FromMinutes(5);

        public Timer KeepAliveTimer { get; protected set; }

        public DateTimeOffset LastCommunicationTime { get; protected set; } = DateTimeOffset.UtcNow;

        public bool Starting { get; protected set; }

        public bool Stopping { get; protected set; }

        public bool EnableKeepAlive
        {
            get => _enableKeepAlive;
            set
            {
                _enableKeepAlive = value;
                if (IsConnected) KeepAliveTimer.Enabled = value;
            }
        }

        public string HostName { get; set; } = IPAddress.Loopback.ToString();

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected => TcpClient?.Connected ?? false;

        public int KeepAliveInterval
        {
            get => (int)KeepAliveTimer.Interval / 1000;
            set
            {
                KeepAliveTimeSpan = TimeSpan.FromSeconds(value);
                KeepAliveTimer.Interval = KeepAliveTimeSpan.TotalMilliseconds;
            }
        }

        public EndPoint LocalEndPoint => TcpClient?.Client?.LocalEndPoint ?? null;

        public ILogger<PotatoClient> Logger { get; protected set; }

        public int Port { get; set; } = 23000;

        public EndPoint RemoteEndPoint => TcpClient?.Client?.RemoteEndPoint ?? null;

        public IWireProtocol WireProtocol { get; protected set; }

        public event ClientConnectionEvent OnConnect;
        public event ClientConnectionEvent OnDisconnect;

        public PotatoClient() : this(new BinaryFormatterWireProtocol(), DefaultLogger) { }

        public PotatoClient(IWireProtocol protocol) : this(protocol, DefaultLogger) { }

        public PotatoClient(ILogger<PotatoClient> logger) : this(new BinaryFormatterWireProtocol(), logger) { }

        protected internal PotatoClient(IWireProtocol protocol, ILogger<PotatoClient> logger, TcpClient client) : this(protocol, logger)
        {
            TcpClient = client;
            HostName = null;
            Port = 0;
        }

        public PotatoClient(IWireProtocol protocol, ILogger<PotatoClient> logger)
        {
            WireProtocol = protocol;
            Logger = logger;

            KeepAliveTimer = new Timer(KeepAliveTimeSpan.TotalMilliseconds) { AutoReset = true };
            KeepAliveTimer.Elapsed += KeepAliveEventHandler;
            KeepAliveMessage = new KeepAlive { Id = Id };

            Logger.LogTrace("PotatoTcp Client initialized");
        }

        public virtual void AddHandler<T>(Action<Guid, T> handler)
        {
            var handlerType = typeof(T);
            Handlers.Add(handlerType, new MessageHandler<T>
            {
                HandlerType = handlerType,
                HandlerAction = handler
            });
        }

        public virtual void AddHandler(IMessageHandler handler)
        {
            Handlers.Add(
                handler.HandlerType,
                handler.MakeClientSpecificCopy(Id));
        }

        public virtual async Task ConnectAsync()
        {
            await ConnectAsync(CancellationToken.None);
        }

        public virtual async Task ConnectAsync(CancellationToken token)
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Connect on a disposed object.");
            if (IsConnected || Starting || Stopping) return;

            try
            {
                Starting = true;

                if (TcpClient == null) TcpClient = new TcpClient(HostName, Port);
                if (!TcpClient.Connected) await TcpClient.ConnectAsync(HostName, Port);

                LastCommunicationTime = DateTimeOffset.UtcNow;
                if (EnableKeepAlive) KeepAliveTimer.Enabled = true;

                StartAsyncSendProcessor(token);

                Starting = false;

                OnConnect?.Invoke(this);
            }
            catch (ObjectDisposedException ode) when (Stopping)
            {
                Logger.LogTrace(ode, $"Ignoring exception because client {Id} is in shutdown mode.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
                throw;
            }
            finally
            {
                Starting = false;
            }
        }

        public virtual void Disconnect()
        {
            if (TcpClient == null || Stopping) return;
            Stopping = true;

            KeepAliveTimer.Enabled = false;

            try
            {
                TcpClient.GetStream().Close();
            }
            catch { }

            TcpClient.Close();

            Stopping = false;
            OnDisconnect?.Invoke(this);
        }

        public void Dispose()
        {
            if (Disposed) return;
            Disposed = true;
            _outgoingMessages.Dispose();
            Disconnect();
        }

        public virtual async Task ListenAsync()
        {
            await ListenAsync(CancellationToken.None);
        }

        public virtual async Task ListenAsync(CancellationToken token)
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Listen on a disposed object.");

            if (!IsConnected || Stopping)
                throw new InvalidOperationException("Cannot listen while client is not connected!");

            await Task.Yield();

            try
            {
                while (!Disposed && IsConnected && !token.IsCancellationRequested)
                {
                    HandleMessage(TcpClient.GetStream());
                }
            }
            catch (InvalidOperationException ioe) when (!IsConnected || Stopping)
            {
                Logger.LogTrace(ioe, $"Ignoring exception because client {Id} is in shutdown mode.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
        }

        public bool RemoteConnectionEstablished()
        {
            var info = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(TcpClient.Client.LocalEndPoint));
            return info != null && info.State == TcpState.Established;
        }

        public virtual void RemoveHandler<T>()
        {
            Handlers.Remove(typeof(T));
        }

        public virtual void Send<T>(T obj) where T : class
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Send<T> on a disposed object.");
            if (!IsConnected) throw new NotConnectedException("The connection has been terminated");

            try
            {
                lock (SendLock)
                {
                    WireProtocol.Serialize(TcpClient.GetStream(), obj);
                    LastCommunicationTime = DateTimeOffset.UtcNow;
                }
            }
            catch (InvalidOperationException ioe) when (Stopping)
            {
                Logger.LogTrace(ioe, $"Ignoring exception because client {Id} is in shutdown mode.");
            }
            catch (IOException) when (!RemoteConnectionEstablished())
            {
                if (IsConnected) Disconnect();
                throw new NotConnectedException("The connection has been terminated");
            }
        }

        public virtual Task SendAsync<T>(T obj) where T : class
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Send<T> on a disposed object.");
            if (!IsConnected) throw new NotConnectedException("The connection has been terminated");

            var source = new TaskCompletionSource<object>();
            _outgoingMessages.Add((source, obj));
            return source.Task;
        }

        private void HandleMessage(Stream stream)
        {
            LastCommunicationTime = DateTimeOffset.UtcNow;

            try
            {
                var message = WireProtocol.Deserialize(stream);

                if (Handlers.TryGetValue(message.GetType(), out IMessageHandler handler))
                {
                    Logger.LogDebug($"Invoking handler for message of type: {message.GetType().FullName}");
                    handler.Invoke(message);
                }
                else
                {
                    Logger.LogDebug($"No handler found for message of type: {message.GetType().FullName ?? "unknown type"}");
                }
            }
            catch (SerializationException) when (!IsConnected || !stream.CanRead || !RemoteConnectionEstablished())
            {
                if (IsConnected) Disconnect();
            }
            // catch (SerializationException se)
            // {
            //     Console.WriteLine($"SerializationException: {se.Message}");
            // }
            catch (IOException ioe) when (!IsConnected || Stopping)
            {
                Logger.LogTrace(ioe, $"Ignoring exception because client {Id} has disconnected.");
            }
            catch (Exception e)
            {
                Logger.LogError(e, e.Message);
            }
        }

        private void KeepAliveEventHandler(object source, System.Timers.ElapsedEventArgs e)
        {
            if ((LastCommunicationTime + KeepAliveTimeSpan) > DateTimeOffset.UtcNow) return;

            if (Disposed || !IsConnected)
            {
                KeepAliveTimer.Enabled = false;
                return;
            }

            try
            {
                Send(KeepAliveMessage);
                LastCommunicationTime = new DateTimeOffset(e.SignalTime.AddMilliseconds(e.SignalTime.Millisecond * -1));
            }
            catch (Exception ex)
            {
                Logger.LogError("Failure while sending keep alive!", ex);
                Disconnect();
            }
        }

        private void StartAsyncSendProcessor(CancellationToken token)
        {
            Task.Run(() =>
            {
                while (!_outgoingMessages.IsCompleted)
                {
                    token.ThrowIfCancellationRequested();
                    (TaskCompletionSource<object> Source, object Message) message = default((TaskCompletionSource<object>, object));

                    try
                    {
                        message = _outgoingMessages.Take(token);
                        Send(message.Message);
                        message.Source.SetResult(message.Message);
                    }
                    catch (NotConnectedException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        message.Source?.SetException(e);
                        Logger.LogDebug(e, "Send message processing thread threw exception");
                    }
                }
            }, token);
        }
    }
}