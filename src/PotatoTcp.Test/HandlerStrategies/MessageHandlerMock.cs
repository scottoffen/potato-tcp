using System;

namespace PotatoTcp.Test.HandlerStrategies
{
    public class MessageHandlerMock : IMessageHandler
    {
        public Guid HandlerGroupId { get; set; } = Guid.NewGuid();

        public Guid ClientId { get; set; } = Guid.NewGuid();

        public Type HandlerType { get; set; } = typeof(Object);

        public bool Invoked => InvokeCounter > 0;

        public int InvokeCounter { get; private set; } = 0;

        public void Invoke(object obj)
        {
            InvokeCounter++;
        }

        public IMessageHandler MakeClientSpecificCopy(Guid clientId)
        {
            throw new NotImplementedException();
        }
    }
}