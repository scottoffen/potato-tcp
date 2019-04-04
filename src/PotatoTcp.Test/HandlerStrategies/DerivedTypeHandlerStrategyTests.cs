using PotatoTcp.HandlerStrategies;
using Samples.Objects.Shapes;
using Shouldly;
using Xunit;

namespace PotatoTcp.Test.HandlerStrategies
{
    public class DerivedTypeHandlerStrategyTests
    {
        DerivedTypeHandlerStrategy strategy = new DerivedTypeHandlerStrategy();

        [Fact]
        public void test()
        {
            var handler = new MessageHandlerMock { HandlerType = typeof(Square) };
            strategy.AddHandler(handler);

            strategy.InvokeHandler(new Rectangle());
            handler.InvokeCounter.ShouldBe(1);

            strategy.InvokeHandler(new Quadrilateral());
            handler.InvokeCounter.ShouldBe(2);

            strategy.TryRemoveHandlers<Rectangle>();

            strategy.InvokeHandler(new Rectangle());
            strategy.InvokeHandler(new Quadrilateral());
            handler.InvokeCounter.ShouldBe(2);

            strategy.InvokeHandler(new Square());
            handler.InvokeCounter.ShouldBe(3);
        }
    }
}