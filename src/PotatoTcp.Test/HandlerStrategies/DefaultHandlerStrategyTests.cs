using System;
using System.Collections.Generic;
using NSubstitute;
using PotatoTcp.HandlerStrategies;
using Shouldly;
using Xunit;

namespace PotatoTcp.Test.HandlerStrategies
{
    public class DefaultHandlerStrategyTests
    {
        private DefaultHandlerStrategy strategy = new DefaultHandlerStrategy();

        [Fact]
        public void add_handler_from_action_adds_type_when_none_exists()
        {
            strategy.Handlers.Count.ShouldBe(0);
            Action<Guid, Object> handler = (guid, obj) => { };

            strategy.AddHandler<Object>(handler);

            strategy.Handlers.Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)].ShouldNotBeNull();
            strategy.Handlers[typeof(Object)].ShouldBeOfType<List<IMessageHandler>>();
            strategy.Handlers[typeof(Object)].Count.ShouldBe(1);

            var msghandler = strategy.Handlers[typeof(Object)][0];
            msghandler.GetType().GetProperty("HandlerAction").GetValue(msghandler, null).ShouldBe(handler);
        }

        [Fact]
        public void add_handler_from_action_adds_multiple_handlers()
        {
            strategy.Handlers.Count.ShouldBe(0);
            Action<Guid, Object> handler1 = (guid, obj) => { };
            Action<Guid, Object> handler2 = (guid, obj) => { };

            strategy.AddHandler<Object>(handler1);
            strategy.AddHandler<Object>(handler2);

            strategy.Handlers.Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)].ShouldNotBeNull();
            strategy.Handlers[typeof(Object)].ShouldBeOfType<List<IMessageHandler>>();
            strategy.Handlers[typeof(Object)].Count.ShouldBe(2);

            var msghandler1 = strategy.Handlers[typeof(Object)][0];
            var msghandler2 = strategy.Handlers[typeof(Object)][1];

            msghandler1.GetType().GetProperty("HandlerAction").GetValue(msghandler1, null).ShouldBe(handler1);
            msghandler2.GetType().GetProperty("HandlerAction").GetValue(msghandler2, null).ShouldBe(handler2);
        }

        [Fact]
        public void add_handler_adds_type_when_none_exists()
        {
            strategy.Handlers.Count.ShouldBe(0);

            var handler = Substitute.For<IMessageHandler>();
            handler.HandlerType.Returns(typeof(Object));

            strategy.AddHandler(handler);

            strategy.Handlers.Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)].ShouldNotBeNull();
            strategy.Handlers[typeof(Object)].ShouldBeOfType<List<IMessageHandler>>();
            strategy.Handlers[typeof(Object)].Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)][0].ShouldBe(handler);
        }

        [Fact]
        public void add_handler_adds_multiple_handlers()
        {
            strategy.Handlers.Count.ShouldBe(0);

            var handler1 = Substitute.For<IMessageHandler>();
            handler1.HandlerType.Returns(typeof(Object));

            var handler2 = Substitute.For<IMessageHandler>();
            handler2.HandlerType.Returns(typeof(Object));

            strategy.AddHandler(handler1);
            strategy.AddHandler(handler2);

            strategy.Handlers.Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)].ShouldNotBeNull();
            strategy.Handlers[typeof(Object)].ShouldBeOfType<List<IMessageHandler>>();
            strategy.Handlers[typeof(Object)].Count.ShouldBe(2);
            strategy.Handlers[typeof(Object)][0].ShouldBe(handler1);
            strategy.Handlers[typeof(Object)][1].ShouldBe(handler2);
        }

        [Fact]
        public void add_handler_adds_handler_when_type_is_specified()
        {
            strategy.Handlers.Count.ShouldBe(0);

            var handler = Substitute.For<IMessageHandler>();
            handler.HandlerType.Returns(typeof(Exception));

            strategy.AddHandler(typeof(Object), handler);

            strategy.Handlers.Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)].ShouldNotBeNull();
            strategy.Handlers[typeof(Object)].ShouldBeOfType<List<IMessageHandler>>();
            strategy.Handlers[typeof(Object)].Count.ShouldBe(1);
            strategy.Handlers[typeof(Object)][0].ShouldBe(handler);
        }
    }
}