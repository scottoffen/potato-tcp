using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PotatoTcp.HandlerStrategies
{
    /// <summary>
    /// Registers handlers to the passed in type.
    /// </summary>
    public class SimpleTypeHandlerStrategy : IHandlerStrategy
    {
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

            _handlers.AddOrUpdate(
                handlerType,
                new List<IMessageHandler> {messageHandler},
                (type, handlers) =>
                {
                    handlers.Add(messageHandler);
                    return handlers;
                });
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

        public bool TryRemoveHandlers<T>()
        {
            return Handlers.Remove(typeof(T));
        }

        public bool TryRemoveHandler<T>(Guid clientId)
        {
            var handlerType = typeof(T);
            if (_handlers.TryGetValue(handlerType, out List<IMessageHandler> handlers))
            {
                handlers.RemoveAll(x => x.ClientId == clientId);
                return handlers.Any() || _handlers.TryRemove(handlerType, out _);
            }
            return false;
        }

        /// <summary>
        /// The message
        /// </summary>
        /// <param name="message"></param>
        /// <returns>True if any handler was found, otherwise false</returns>
        public bool InvokeHandler(object message)
        {
            if (_handlers.TryGetValue(message.GetType(), out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }

            return false;
        }
    }
}