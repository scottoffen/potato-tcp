using System;

namespace PotatoTcp
{
    public interface IMessageHandler
    {
        Guid HandlerGroupId { get; set; }
        Guid ClientId { get; set; }
        Type HandlerType { get; set; }
        void Invoke(object obj);

        IMessageHandler MakeClientSpecificCopy(Guid clientId);
    }

    internal class MessageHandler<T> : IMessageHandler
    {
        public Action<Guid, T> HandlerAction { get; set; }

        public Guid HandlerGroupId { get; set; }
        public Guid ClientId { get; set; }
        public Type HandlerType { get; set; }

        public void Invoke(object obj)
        {
            HandlerAction(ClientId, (T)obj);
        }

        public IMessageHandler MakeClientSpecificCopy(Guid clientId)
        {
            return new MessageHandler<T>
            {
                ClientId = clientId,
                HandlerType = HandlerType,
                HandlerAction = HandlerAction
            };
        }
    }
}