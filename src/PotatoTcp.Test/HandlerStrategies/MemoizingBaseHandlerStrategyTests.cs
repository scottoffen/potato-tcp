using PotatoTcp.HandlerStrategies;
using Samples.Objects.Shapes;
using Shouldly;
using Xunit;

namespace PotatoTcp.Test.HandlerStrategies
{
    public class MemoizingBaseHandlerStrategyTests
    {
        private MemoizingBaseHandlerStrategy strategy = new MemoizingBaseHandlerStrategy();

        [Fact]
        public void test()
        {
            var handler = new MessageHandlerMock { HandlerType = typeof(Rectangle) };
            strategy.AddHandler(handler);

            strategy.InvokeHandler(new Square());
            handler.InvokeCounter.ShouldBe(1);

            strategy.InvokeHandler(Square.GetTinySquare());
            handler.InvokeCounter.ShouldBe(2);
        }
    }
}