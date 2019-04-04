using System;

namespace PotatoTcp.HandlerStrategies
{
    /// <summary>
    /// Registers handler to its type and all base types.
    /// </summary>
    public class DerivedTypeHandlerStrategy : DefaultHandlerStrategy, IHandlerStrategy
    {
        public override void AddHandler(IMessageHandler handler)
        {
            if (handler.HandlerGroupId == Guid.Empty) handler.HandlerGroupId = Guid.NewGuid();
            base.AddHandler(handler);

            foreach (var baseType in handler.HandlerType.GetBaseTypes())
            {
                base.AddHandler(baseType, handler);
            }
        }

        public override bool TryRemoveHandlers<T>()
        {
            var handlerType = typeof(T);
            var groupIds = GetHandlerGroupIds<T>();

            if (base.TryRemoveHandlers<T>())
            {
                foreach (var baseType in handlerType.GetBaseTypes())
                {
                    base.TryRemoveHandlersByGroup(baseType, groupIds);
                }
            }

            return false;
        }

        public override bool TryRemoveHandlersByClient<T>(Guid clientId)
        {
            if (base.TryRemoveHandlersByClient<T>(clientId))
            {
                foreach (var baseType in typeof(T).GetBaseTypes())
                {
                    base.TryRemoveHandlersByClient(baseType, clientId);
                }
            }

            return false;
        }
    }
}