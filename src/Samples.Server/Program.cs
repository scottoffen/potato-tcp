using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PotatoTcp.Client;
using PotatoTcp.Serialization;
using PotatoTcp.Server;
using Samples.Objects;

namespace Samples.Server
{
    public class Program
    {
        public static ServiceProvider container = BuildContainer();

        public static void Main(string[] args)
        {
            Console.WriteLine("****************************************************");
            Console.WriteLine("*          PotatoTcp Command Line Utility          *");
            Console.WriteLine("****************************************************");
            Console.Write("Press enter to start the server");
            Console.ReadLine();

            using (var server = Configure())
            {
                Console.WriteLine();
                Console.WriteLine("Enter <x> to exit the utility at any time.");
                Console.WriteLine("Enter <l> to send a King Lear to the server.");
                Console.WriteLine("Enter <s> to send a person object to all clients.");

                var breaker = true;
                while (breaker)
                {
                    try
                    {
                        var input = Console.ReadLine().Trim().ToLower().Substring(0, 1);
                        switch (input)
                        {
                            case "l":
                                Console.WriteLine($"Sending: King Lear");
                                server.Send(new LargeDataObject());
                                break;
                            case "s":
                                var person = Person.Create();
                                Console.WriteLine($"Sending: {person}");
                                server.Send(person);
                                break;
                            case "x":
                                breaker = false;
                                break;
                            default:
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        breaker = false;
                    }
                }
            }

            NLog.LogManager.Shutdown();

            Console.WriteLine("Goodbye");
            Console.ReadLine();
        }

        private static IPotatoServer Configure()
        {
            Console.Write("Starting server...");
            var server = container.GetRequiredService<IPotatoServer>();

            // Register Event Handlers (optional)
            server.OnStart += (s) => s.Logger.LogInformation("Server OnStart event handler fired.");
            server.OnStop += (s) => s.Logger.LogInformation("Server OnStop event handler fired.");
            server.OnClientConnect += (c) => c.Logger.LogInformation($"Connection established with {c.RemoteEndPoint}");
            server.OnClientConnect += (c) => Console.WriteLine($"Connection established with {c.RemoteEndPoint}");

            // Register Message Handlers
            server.AddHandler<Person>(Person.Handler);
            server.AddHandler<LargeDataObject>(LargeDataObject.Handler);

            // You can configure the keep alive to send whatever you want,
            // but you will need to create and register your own handler for it.
            server.AddHandler<KeepAlive>(KeepAliveHandler);

            // Start Listening
            server.StartAsync();
            Console.WriteLine("done");
            Console.WriteLine($"Server listening on {server.IpEndpoint.Address}:{server.IpEndpoint.Port}");

            return server;
        }

        private static ServiceProvider BuildContainer()
        {
            return new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                // You can inject your own wire protocol
                .AddSingleton<IWireProtocol, LengthPrefixedWireProtocol>()
                // Injecting the default factory allows for a logger to be injected into generated clients
                .AddSingleton<IPotatoClientFactory, PotatoClientFactory>()
                .AddSingleton<IPotatoServer, PotatoServer>()
                .BuildServiceProvider();
        }

        private static Action<Guid, KeepAlive> KeepAliveHandler = (Guid guid, KeepAlive obj) =>
        {
            Console.WriteLine($"Received: {obj.Message} from {guid}");
        };
    }
}
