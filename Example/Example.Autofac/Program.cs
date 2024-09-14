﻿using NanoRabbit.DependencyInjection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Example.Autofac;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NanoRabbit;
using NanoRabbit.Connection;
using NanoRabbit.Autofac;

try
{
    var host = CreateHostBuilder(args).Build();
    await host.RunAsync();
}
catch (Exception)
{
    throw;
}

IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
    .UseServiceProviderFactory(new AutofacServiceProviderFactory())
    .ConfigureContainer<ContainerBuilder>((context, builders) =>
    {
        // ...
        builders.RegisterRabbitHelper(builder =>
        {
            builder.SetHostName("localhost")
                .SetPort(5672)
                .SetVirtualHost("/")
                .SetUserName("admin")
                .SetPassword("admin")
                .AddProducerOption(producer =>
                {
                    producer.ProducerName = "FooProducer";
                    producer.ExchangeName = "amq.topic";
                    producer.RoutingKey = "foo.key";
                    producer.Type = ExchangeType.Topic;
                })
                .AddProducerOption(producer =>
                {
                    producer.ProducerName = "BarProducer";
                    producer.ExchangeName = "amq.direct";
                    producer.RoutingKey = "bar.key";
                    producer.Type = ExchangeType.Direct;
                })
                .AddConsumerOption(consumer =>
                {
                    consumer.ConsumerName = "FooConsumer";
                    consumer.QueueName = "foo-queue";
                })
                .AddConsumerOption(consumer =>
                {
                    consumer.ConsumerName = "BarConsumer";
                    consumer.QueueName = "bar-queue";
                });
        });
        builders.RegisterRabbitConsumer<FooQueueHandler>("FooConsumer");
    })
    .ConfigureServices((context, services) =>
    {
        // register BackgroundService
        services.AddHostedService<PublishService>();
    });

public class FooQueueHandler : DefaultMessageHandler
{
    public override void HandleMessage(string message)
    {
        Console.WriteLine($"[x] Received from foo-queue: {message}");
        Task.Delay(1000).Wait();
        Console.WriteLine("[x] Done");
    }
}

public class BarQueueHandler : DefaultMessageHandler
{
    public override void HandleMessage(string message)
    {
        Console.WriteLine($"[x] Received from bar-queue: {message}");
        Task.Delay(500).Wait();
        Console.WriteLine("[x] Done");
    }
}