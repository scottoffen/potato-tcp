using System;
using System.Collections.Generic;

namespace PotatoTcp.HandlerStrategies
{
    public interface IHandlerStrategy
    {
        void AddHandler<T>(Action<Guid, T> handler);

        void AddHandler(IMessageHandler handler);

        /// <summary>
        /// The message
        /// </summary>
        /// <param name="message"></param>
        /// <returns>True if any handler was found, otherwise false</returns>
        bool InvokeHandler(object message);

        bool TryRemoveHandlers<T>();

        bool TryRemoveHandlersByClient<T>(Guid clientId);

        bool TryRemoveHandlersByGroup<T>(IEnumerable<Guid> groupIds);
    }
}