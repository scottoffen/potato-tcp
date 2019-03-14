using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PotatoTcp.HandlerStrategies
{
    public class DefaultHandlerStrategy : IHandlerStrategy
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

        public void RemoveHandlers<T>()
        {
            Handlers.Remove(typeof(T));
        }

        public void RemoveHandler<T>(Guid clientId)
        {
            if (_handlers.TryGetValue(typeof(T), out List<IMessageHandler> handlers))
            {
                handlers.RemoveAll(x => x.ClientId == clientId);
            }
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