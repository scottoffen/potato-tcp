using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PotatoTcp.HandlerStrategies
{

    /// <summary>
    /// Registers handler to its type and all base types.
    /// </summary>
    public class DerivedTypeHandlerStrategy : IHandlerStrategy
    {
        private readonly ConcurrentDictionary<Type, List<IMessageHandler>> _handlers = 
            new ConcurrentDictionary<Type, List<IMessageHandler>>();

        public IDictionary<Type, List<IMessageHandler>> Handlers => _handlers;

        public void AddHandler<T>(Action<Guid, T> handler)
        {
            var handlerType = typeof(T);
            var messageHandler = new MessageHandler<T>
            {
                HandlerGroupId = Guid.NewGuid(),
                HandlerType = handlerType,
                HandlerAction = handler
            };

            AddHandlerInternal(handlerType, messageHandler);
        }

        public void AddHandler(IMessageHandler handler)
        {
            if (handler.HandlerGroupId == Guid.Empty)
            {
                handler.HandlerGroupId = Guid.NewGuid();
            }
            AddHandlerInternal(handler.HandlerType, handler);
        }

        public bool InvokeHandler(object message)
        {
            if (_handlers.TryGetValue(message.GetType(), out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }
            return false;
        }

        public bool TryRemoveHandlers<T>()
        {
            var handlerType = typeof(T);
            if (_handlers.TryRemove(handlerType, out List<IMessageHandler> handlers))
            {
                var groupIds = handlers.Select(x => x.HandlerGroupId);
                var allBasesRemoved = true;
                foreach (var baseType in handlerType.GetBaseTypes())
                {
                    if (_handlers.TryGetValue(baseType, out List<IMessageHandler> baseHandlers))
                    {
                        baseHandlers.RemoveAll(x => groupIds.Contains(x.HandlerGroupId));

                        if (!baseHandlers.Any())
                        {
                            if (!_handlers.TryRemove(baseType, out _))
                            {
                                allBasesRemoved = false;
                            }
                        }
                    }
                }
                return allBasesRemoved;
            }
            return false;
        }

        public bool TryRemoveHandler<T>(Guid clientId)
        {
            var handlerType = typeof(T);
            if (_handlers.TryGetValue(handlerType, out List<IMessageHandler> handlers))
            {
                var removedHandlers = handlers.RemoveAll(x => x.ClientId == clientId);
                if (!handlers.Any())
                {
                    _handlers.TryRemove(handlerType, out _);
                }

                foreach (var baseType in handlerType.GetBaseTypes())
                {
                    if (_handlers.TryGetValue(baseType, out List<IMessageHandler> baseHandlers))
                    {
                        removedHandlers += baseHandlers.RemoveAll(x => x.ClientId == clientId);
                        if (!baseHandlers.Any())
                        {
                            if (_handlers.TryRemove(baseType, out _))
                            {
                                removedHandlers++;
                            }
                        }
                    }
                }

                return removedHandlers > 0;
            }
            return false;
        }

        private void AddHandlerInternal(Type handlerType, IMessageHandler messageHandler)
        {
            _handlers.AddOrUpdate(
                handlerType,
                new List<IMessageHandler> { messageHandler },
                (type, handlers) =>
                {
                    handlers.Add(messageHandler);
                    return handlers;
                });

            foreach (var baseType in handlerType.GetBaseTypes())
            {
                _handlers.AddOrUpdate(
                    baseType,
                    new List<IMessageHandler> { messageHandler },
                    (type, handlers) =>
                    {
                        handlers.Add(messageHandler);
                        return handlers;
                    });
            }
        }
    }
}