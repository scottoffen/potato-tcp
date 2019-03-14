using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace PotatoTcp.HandlerStrategies
{
    public class DerivedTypeHandlerStrategy : IHandlerStrategy
    {
        private readonly ConcurrentDictionary<Type, List<IMessageHandler>> _handlers = new ConcurrentDictionary<Type, List<IMessageHandler>>();
        private readonly ConcurrentDictionary<Type, List<Type>> _alternateTypes = new ConcurrentDictionary<Type, List<Type>>();

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
            var messageType = GetType();
            if (_handlers.TryGetValue(messageType, out List<IMessageHandler> handlers))
            {
                handlers.ForEach(handler => handler.Invoke(message));
                return true;
            }

            if (_alternateTypes.TryGetValue(messageType, out List<Type> baseTypes))
            {
                var handlerFound = false;
                foreach (var baseType in baseTypes)
                {
                    if (_handlers.TryGetValue(baseType, out List<IMessageHandler> baseHandlers))
                    {
                        handlerFound = true;
                        baseHandlers.ForEach(handler => handler.Invoke(message));
                    }
                }

                return handlerFound;
            }

            var msgBaseTypes = GetBaseTypes(messageType).Union(_handlers.Keys);
            _alternateTypes.AddOrUpdate(messageType, msgBaseTypes.ToList(), (_, types) => types);
            return InvokeHandler(message);
        }

        private IEnumerable<Type> GetBaseTypes(Type handlerType)
        {
            var objType = typeof(object);
            var baseType = handlerType.BaseType;
            while (baseType != null && baseType != objType)
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
    }
}