using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PotatoTcp.HandlerStrategies
{
    public class DefaultHandlerStrategy : IHandlerStrategy
    {
        private readonly ConcurrentDictionary<Type, List<IMessageHandler>> _handlers = new ConcurrentDictionary<Type, List<IMessageHandler>>();

        public IDictionary<Type, List<IMessageHandler>> Handlers => _handlers;

        public virtual void AddHandler<T>(Action<Guid, T> handler)
        {
            var messageHandler = new MessageHandler<T>
            {
                HandlerType = typeof(T),
                HandlerAction = handler
            };

            AddHandler(messageHandler.HandlerType, messageHandler);
        }

        public virtual void AddHandler(IMessageHandler handler)
        {
            AddHandler(handler.HandlerType, handler);
        }

        public void AddHandler(Type type, IMessageHandler handler)
        {
            _handlers.AddOrUpdate(
                type,
                new List<IMessageHandler> { handler },
                (handlerType, handlers) =>
                {
                    handlers.Add(handler);
                    return handlers;
                });
        }

        public IEnumerable<Guid> GetHandlerGroupIds<T>()
        {
            _handlers.TryGetValue(typeof(T), out List<IMessageHandler> handlers);
            return handlers.Select(x => x.HandlerGroupId);
        }

        public virtual bool InvokeHandler(object message)
        {
            if (Handlers.TryGetValue(message.GetType(), out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }

            return false;
        }

        public bool InvokeHandler(Type type, object message)
        {
            if (!type.IsAssignableFrom(message.GetType())) throw new ArgumentOutOfRangeException(nameof(type), $"{type} is not assignable from {message.GetType()}");

            if (Handlers.TryGetValue(type, out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }

            return false;
        }

        public virtual bool TryRemoveHandlers<T>()
        {
            return TryRemoveHandlers(typeof(Type));
        }

        public bool TryRemoveHandlers(Type type)
        {
            return _handlers.TryRemove(type, out _);
        }

        public virtual bool TryRemoveHandlersByClient<T>(Guid clientId)
        {
            return TryRemoveHandlersByClient(typeof(T), clientId);
        }

        public bool TryRemoveHandlersByClient(Type type, Guid clientId)
        {
            if (_handlers.TryGetValue(type, out List<IMessageHandler> handlers))
            {
                handlers.RemoveAll(x => x.ClientId == clientId);
                return handlers.Any() || _handlers.TryRemove(type, out _);
            }

            return false;
        }

        public virtual bool TryRemoveHandlersByGroup<T>(IEnumerable<Guid> groupIds)
        {
            return TryRemoveHandlersByGroup(typeof(T), groupIds);
        }

        public bool TryRemoveHandlersByGroup(Type type, IEnumerable<Guid> groupIds)
        {
            if (_handlers.TryGetValue(type, out List<IMessageHandler> handlers))
            {
                handlers.RemoveAll(x => groupIds.Contains(x.HandlerGroupId));
                return handlers.Any() || _handlers.TryRemove(type, out _);
            }

            return false;
        }
    }
}