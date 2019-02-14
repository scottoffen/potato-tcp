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
        public const int DefaultKeepAliveInterval = 300000; // 5 minutes

        public static readonly ILogger<PotatoClient> DefaultLogger = new NullLoggerFactory().CreateLogger<PotatoClient>();

        private readonly BlockingCollection<(TaskCompletionSource<object>, Envelope, object)> _outgoingMessages = new BlockingCollection<(TaskCompletionSource<object>, Envelope, object)>();
        private bool enableKeepAlive = false;

        protected readonly object SendLock = new object();
        protected bool Starting { get; set; }
        protected bool Stopping { get; set; }
        public ILogger<PotatoClient> Logger { get; protected set; }
        protected TcpClient TcpClient;
        protected bool Disposed { get; private set; }
        protected IDictionary<Type, IMessageHandler> Handlers { get; } = new ConcurrentDictionary<Type, IMessageHandler>();
        protected Timer KeepAliveTimer { get; set; }
        protected TimeSpan KeepAliveTimeSpan { get; set; } = new TimeSpan(0, 0, DefaultKeepAliveInterval);
        protected readonly Envelope KeepAliveEnvelope = new Envelope
        {
            MessageType = MessageType.Ping,
            DataType = typeof(Object).AssemblyQualifiedName,
            Data = new MemoryStream()
        };

        public DateTimeOffset LastCommunicationTime { get; private set; } = new DateTimeOffset(DateTime.UtcNow);

        public int KeepAliveInterval
        {
            get => (int)KeepAliveTimer.Interval / 1000;
            set
            {
                KeepAliveTimer.Interval = value * 1000;
                KeepAliveTimeSpan = new TimeSpan(0, 0, (int)value);
            }
        }

        public bool EnableKeepAlive
        {
            get => enableKeepAlive;
            set
            {
                enableKeepAlive = value;
                if (IsConnected) KeepAliveTimer.Enabled = value;
            }
        }

        public event ClientConnectionEvent OnConnect;
        public event ClientConnectionEvent OnDisconnect;

        public string HostName { get; set; } = "127.0.0.1";
        public Guid Id { get; } = Guid.NewGuid();
        public bool IsConnected => TcpClient != null && TcpClient.Connected;
        public EndPoint RemoteEndPoint => TcpClient?.Client?.RemoteEndPoint;
        public EndPoint LocalEndPoint => TcpClient?.Client?.LocalEndPoint;

        public int Port { get; set; } = 23000;

        public IEnvelopeSerializer EnvelopeSerializer { get; protected set; }

        public IMessageSerializer MessageSerializer { get; protected set; }

        public PotatoClient() : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), DefaultLogger) { }

        public PotatoClient(ILogger<PotatoClient> logger) : this(new BinaryEnvelopeSerializer(), new BinaryMessageSerializer(), logger) { }

        public PotatoClient(IEnvelopeSerializer envelopeSerializer) : this(envelopeSerializer, new BinaryMessageSerializer(), DefaultLogger) { }

        public PotatoClient(IMessageSerializer messageSerializer) : this(new BinaryEnvelopeSerializer(), messageSerializer, DefaultLogger) { }

        public PotatoClient(IEnvelopeSerializer envelopeSerializer, ILogger<PotatoClient> logger) : this(envelopeSerializer, new BinaryMessageSerializer(), logger) { }

        public PotatoClient(IMessageSerializer messageSerializer, ILogger<PotatoClient> logger) : this(new BinaryEnvelopeSerializer(), messageSerializer, logger) { }

        public PotatoClient(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer) : this(envelopeSerializer, messageSerializer, DefaultLogger) { }

        protected internal PotatoClient(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer, ILogger<PotatoClient> logger, TcpClient client) : this(envelopeSerializer, messageSerializer, logger)
        {
            TcpClient = client;
        }

        public PotatoClient(IEnvelopeSerializer envelopeSerializer, IMessageSerializer messageSerializer, ILogger<PotatoClient> logger)
        {
            EnvelopeSerializer = envelopeSerializer;
            MessageSerializer = messageSerializer;
            Logger = logger;

            KeepAliveTimer = new Timer(DefaultKeepAliveInterval) { AutoReset = true };
            KeepAliveTimer.Elapsed += KeepAliveEventHandler;

            KeepAliveEnvelope.ClientId = Id;

            Logger.LogTrace("PotatoTcp Client initialized.");
        }

        public void AddHandler<T>(Action<Guid, T> handler)
        {
            var handlerType = typeof(T);
            Handlers.Add(handlerType, new MessageHandler<T>
            {
                HandlerType = handlerType,
                HandlerAction = handler
            });
        }

        public void AddHandler(IMessageHandler handler)
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

                LastCommunicationTime = new DateTimeOffset(DateTime.UtcNow);
                if (EnableKeepAlive) KeepAliveTimer.Enabled = true;

                Starting = false;

                OnConnect?.Invoke(this);

                ListenAsync(token);
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

            KeepAliveTimer.Enabled = false;

            Stopping = true;
            TcpClient.Close();
            Stopping = false;

            OnDisconnect?.Invoke(this);
        }

        public void Dispose()
        {
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

            StartAsyncSendProcessor(token);

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

        public void RemoveHandler<T>()
        {
            Handlers.Remove(typeof(T));
        }

        public virtual void Send<T>(T obj)
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Send<T> on a disposed object.");
            if (!IsConnected) throw new NotConnectedException("The connection has been terminated");

            var envelope = new Envelope
            {
                MessageType = MessageType.Content,
                DataType = typeof(T).AssemblyQualifiedName,
                Data = new MemoryStream()
            };

            SendEnvelope(envelope, obj);
        }

        public virtual Task SendAsync<T>(T obj)
        {
            if (Disposed) throw new ObjectDisposedException("Can't call Send<T> on a disposed object.");
            if (!IsConnected) throw new NotConnectedException("The connection has been terminated");

            var envelope = new Envelope
            {
                MessageType = MessageType.Content,
                DataType = typeof(T).AssemblyQualifiedName,
                Data = new MemoryStream()
            };

            var source = new TaskCompletionSource<object>();
            _outgoingMessages.Add((source, envelope, obj));
            return source.Task;
        }

        public bool RemoteConnectionEstablished()
        {
            var info = IPGlobalProperties.GetIPGlobalProperties()
                .GetActiveTcpConnections()
                .SingleOrDefault(x => x.LocalEndPoint.Equals(TcpClient.Client.LocalEndPoint));
            return info != null && info.State == TcpState.Established;
        }

        private void HandleMessage(Stream stream)
        {
            LastCommunicationTime = new DateTimeOffset(DateTime.UtcNow);

            try
            {
                var envelope = EnvelopeSerializer.Deserialize(stream);
                if (envelope.MessageType == MessageType.Ping)
                {
                    Logger.LogDebug($"Ping received from {TcpClient.Client.RemoteEndPoint} on {TcpClient.Client.LocalEndPoint}");
                    return;
                }

                var msgType = Type.GetType(envelope.DataType);

                if (Handlers.TryGetValue(msgType, out IMessageHandler handler))
                {
                    Logger.LogDebug($"Invoking handler for message of type: {msgType.FullName}");
                    handler.Invoke(MessageSerializer.Deserialize(msgType, envelope.Data));
                }
                else
                {
                    Logger.LogDebug($"No handler found for message of type: {msgType?.FullName}");
                }
            }
            catch (SerializationException) when (!RemoteConnectionEstablished())
            {
                if (IsConnected) Disconnect();
            }
            catch (IOException ioe) when (!IsConnected)
            {
                Logger.LogTrace(ioe, $"Ignoring exception because client {Id} has disconnected.");
            }
            catch (Exception e)
            {
                /*
                  An exception was thrown by either:
                  - envelope deserializer
                  - type deserializer
                  - message deserializer
                  - invoked handler
                 */
                Logger.LogError(e, e.Message);
            }
        }

        private void KeepAliveEventHandler(Object source, System.Timers.ElapsedEventArgs e)
        {
            var now = new DateTimeOffset(e.SignalTime.AddMilliseconds(e.SignalTime.Millisecond * -1));

            if ((LastCommunicationTime + KeepAliveTimeSpan) > new DateTimeOffset(DateTime.UtcNow)) return;

            if (Disposed || !IsConnected)
            {
                KeepAliveTimer.Enabled = false;
                return;
            }

            try
            {
                SendEnvelope(KeepAliveEnvelope, new object());
                LastCommunicationTime = now;
            }
            catch (Exception ex)
            {
                Logger.LogError("Failure while sending ping message", ex);
                Disconnect();
            }
        }

        private void SendEnvelope(Envelope envelope, object obj)
        {
            try
            {
                lock (SendLock)
                {
                    MessageSerializer.Serialize(envelope.Data, obj);
                    EnvelopeSerializer.Serialize(TcpClient.GetStream(), envelope);
                    LastCommunicationTime = new DateTimeOffset(DateTime.UtcNow);
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

        private void StartAsyncSendProcessor(CancellationToken token)
        {
            Task.Run(() =>
            {
                while (!_outgoingMessages.IsCompleted)
                {
                    token.ThrowIfCancellationRequested();
                    (TaskCompletionSource<object> Source, Envelope Envelope, object Message) messageData = default((TaskCompletionSource<object>, Envelope, object));
                    try
                    {
                        messageData = _outgoingMessages.Take(token);
                        SendEnvelope(messageData.Envelope, messageData.Message);
                        messageData.Source.SetResult(messageData.Message);
                    }
                    catch (NotConnectedException)
                    {
                        throw;
                    }
                    catch (Exception e)
                    {
                        messageData.Source?.SetException(e);
                        //This error should be handled by the client but if they 'fire and forget'
                        //this should give some indication as to what is happening.
                        Logger.LogDebug(e, "Send message processing thread threw exception");
                    }
                }
            }, token);
        }
    }
}