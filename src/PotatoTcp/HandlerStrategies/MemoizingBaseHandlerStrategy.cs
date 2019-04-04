using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace PotatoTcp.HandlerStrategies
{
    public class MemoizingBaseHandlerStrategy : DefaultHandlerStrategy, IHandlerStrategy
    {
        private readonly ConcurrentDictionary<Type, IEnumerable<Type>> _handlerTypes = new ConcurrentDictionary<Type, IEnumerable<Type>>();

        public override bool InvokeHandler(object message)
        {
            if (base.InvokeHandler(message)) return true;

            var handled = false;
            var messageType = message.GetType();
            IEnumerable<Type> baseHandlerTypes;

            if (!_handlerTypes.TryGetValue(messageType, out baseHandlerTypes))
            {
                baseHandlerTypes = messageType.GetBaseTypes();
                _handlerTypes.AddOrUpdate(messageType, baseHandlerTypes, (k, v) => baseHandlerTypes);
            }

            foreach (var type in baseHandlerTypes)
            {
                if (base.InvokeHandler(type, message)) handled = true;
            }

            return handled;
        }
    }
}