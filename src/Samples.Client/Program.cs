﻿using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using PotatoTcp.Client;
using Samples.Objects;

namespace Samples.Client
{
    public class Program
    {
        public static ServiceProvider container = BuildContainer();

        public static void Main(string[] args)
        {
            Console.WriteLine("****************************************************");
            Console.WriteLine("*          PotatoTcp Command Line Utility          *");
            Console.WriteLine("****************************************************");
            Console.Write("Press enter to start the client");
            Console.ReadLine();

            using (var client = Configure())
            {
                Console.WriteLine();
                Console.WriteLine("Enter <x> to exit the utility at any time.");
                Console.WriteLine("Enter <s> to send a person object to the server.");

                var breaker = true;
                while (breaker)
                {
                    try
                    {
                        var input = Console.ReadLine().Trim().ToLower().Substring(0, 1);
                        switch (input)
                        {
                            case "s":
                                var person = Person.Create();
                                Console.WriteLine($"Sending: {person}");
                                client.Send(person);
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

        private static IPotatoClient Configure()
        {
            Console.Write("Starting client...");
            var client = container.GetRequiredService<IPotatoClient>();

            // Change where the client connects to here:
            // client.HostName = "127.0.0.1";
            // client.Port = 23000;

            client.OnConnect += (c) => c.Logger.LogInformation($"Connection established with {c.RemoteEndPoint}");
            client.OnDisconnect += (c) => client.Logger.LogInformation($"Disconnected from server");

            client.AddHandler<Person>(Person.Handler);

            client.ConnectAsync().Wait();
            Console.WriteLine("done");
            Console.WriteLine($"Client connected to {client.RemoteEndPoint}");

            return client;
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
                .AddScoped<IPotatoClient, PotatoClient>()
                .BuildServiceProvider();
        }
    }
}