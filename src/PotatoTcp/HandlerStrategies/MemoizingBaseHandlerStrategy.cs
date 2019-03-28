using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PotatoTcp.HandlerStrategies
{
    public class MemoizingBaseHandlerStrategy : IHandlerStrategy
    {
        private readonly ConcurrentDictionary<Type, Type> _handlerTypes = new ConcurrentDictionary<Type, Type>();
        private readonly ConcurrentDictionary<Type, List<IMessageHandler>> _handlers = new ConcurrentDictionary<Type, List<IMessageHandler>>();

        public IDictionary<Type, List<IMessageHandler>> Handlers => _handlers;

        public void AddHandler<T>(Action<Guid, T> handler)
        {
            var handlerType = typeof(T);
            var messageHandler = new MessageHandler<T>
            {
                HandlerType = handlerType,
                HandlerAction = handler
            };

            AddHandler(messageHandler);
        }

        public void AddHandler(IMessageHandler handler)
        {
            _handlers.AddOrUpdate(
                handler.HandlerType,
                new List<IMessageHandler> {handler},
                (type, handlers) =>
                {
                    handlers.Add(handler);
                    return handlers;
                });
        }

        public bool InvokeHandler(object message)
        {
            var messageType = message.GetType();

            if (TryInvokeHandler(messageType, message)) return true;

            if (_handlerTypes.TryGetValue(messageType, out Type baseHandlerType))
            {
                return TryInvokeHandler(baseHandlerType, message);
            }

            var firstBaseType = messageType.GetBaseTypes().FirstOrDefault(x => _handlers.TryGetValue(x, out _));
            _handlerTypes.AddOrUpdate(messageType, _ => firstBaseType, (_, __) => firstBaseType);
            return TryInvokeHandler(firstBaseType, message);
        }

        public bool TryRemoveHandlers<T>()
        {
            var handlerType = typeof(T);

            if (_handlers.TryRemove(handlerType, out _))
            {
                foreach (var kvp in _handlerTypes.Where(x => x.Value == handlerType))
                {
                    _handlerTypes.TryRemove(kvp.Key, out _);
                }
                return true;
            }
            return false;
        }

        public bool TryRemoveHandler<T>(Guid clientId)
        {
            var handlerType = typeof(T);
            if (_handlers.TryGetValue(handlerType, out List<IMessageHandler> handlers))
            {
                handlers.RemoveAll(x => x.ClientId == clientId);

                if (!handlers.Any())
                {
                    _handlers.TryRemove(handlerType, out _);
                    foreach (var kvp in _handlerTypes.Where(x => x.Value == handlerType))
                    {
                        _handlerTypes.TryRemove(kvp.Key, out _);
                    }
                    return true;
                }
                return handlers.Any() || _handlers.TryRemove(handlerType, out _);
            }
            return false;
        }

        private bool TryInvokeHandler(Type messageType, object message)
        {
            if (_handlers.TryGetValue(messageType, out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }
            return false;
        }
    }
}