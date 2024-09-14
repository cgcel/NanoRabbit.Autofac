using Autofac;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NanoRabbit.Connection;

namespace NanoRabbit.Autofac
{
    public static class RabbitHelperExtensions
    {
        /// <summary>
        /// Register a singleton service of the type specified in IRabbitHelper with a factory specified in implementationFactory to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configurationBuilder"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterRabbitHelper(this ContainerBuilder builder, Action<RabbitConfigurationBuilder> configurationBuilder, Func<IComponentContext, ILogger>? loggerFactory = null)
        {
            var rabbitConfigBuilder = new RabbitConfigurationBuilder();
            configurationBuilder.Invoke(rabbitConfigBuilder);
            var rabbitConfig = rabbitConfigBuilder.Build();

            builder.Register(c =>
            {
                var logger = loggerFactory?.Invoke(c) ?? NullLogger.Instance;
                return new RabbitHelper(rabbitConfig, logger);
            }).As<IRabbitHelper>().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register a keyed singleton service of the type specified in IRabbitHelper with a factory specified in implementationFactory to the specified Microsoft.Extensions.DependencyInjection.IServiceCollection.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="key"></param>
        /// <param name="configurationBuilder"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterKeyedRabbitHelper(this ContainerBuilder builder, string key, Action<RabbitConfigurationBuilder> configurationBuilder, Func<IComponentContext, ILogger>? loggerFactory = null)
        {
            var rabbitConfigBuilder = new RabbitConfigurationBuilder();
            configurationBuilder.Invoke(rabbitConfigBuilder);
            var rabbitConfig = rabbitConfigBuilder.Build();

            builder.Register(c =>
            {
                var logger = loggerFactory?.Invoke(c) ?? NullLogger.Instance;
                return new RabbitHelper(rabbitConfig, logger);
            }).Keyed<IRabbitHelper>(key).SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register a singleton service of the type specified in IRabbitHelper by reading configurations of appsettings.json.
        /// </summary>
        /// <typeparam name="TRabbitConfiguration"></typeparam>
        /// <param name="builder"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ContainerBuilder RegisterRabbitHelperFromAppSettings<TRabbitConfiguration>(this ContainerBuilder builder, IConfiguration configuration, Func<IComponentContext, ILogger>? loggerFactory = null)
            where TRabbitConfiguration : RabbitConfiguration, new()
        {
            var rabbitConfig = NanoRabbit.DependencyInjection.RabbitHelperExtensions.ReadSettings<TRabbitConfiguration>(configuration);

            if (rabbitConfig == null)
            {
                throw new Exception("NanoRabbit Configuration is incorrect.");
            }

            builder.Register(c =>
            {
                var logger = loggerFactory?.Invoke(c) ?? NullLogger.Instance;
                return new RabbitHelper(rabbitConfig, logger);
            }).As<IRabbitHelper>().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register a keyed singleton service of the type specified in IRabbitHelper by reading configurations of appsettings.json.
        /// </summary>
        /// <typeparam name="TRabbitConfiguration"></typeparam>
        /// <param name="builder"></param>
        /// <param name="key"></param>
        /// <param name="configuration"></param>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static ContainerBuilder RegisterKeyedRabbitHelperFromAppSettings<TRabbitConfiguration>(this ContainerBuilder builder, string key, IConfiguration configuration, Func<IComponentContext, ILogger>? loggerFactory = null)
        where TRabbitConfiguration : RabbitConfiguration, new()
        {
            var rabbitConfig = NanoRabbit.DependencyInjection.RabbitHelperExtensions.ReadSettings<TRabbitConfiguration>(configuration);

            if (rabbitConfig == null)
            {
                throw new Exception("NanoRabbit Configuration is incorrect.");
            }

            builder.Register(c =>
            {
                var logger = loggerFactory?.Invoke(c) ?? NullLogger.Instance;
                return new RabbitHelper(rabbitConfig, logger);
            }).Keyed<IRabbitHelper>(key).SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register a consumer to specific IRabbitHelper.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="builder"></param>
        /// <param name="consumerName"></param>
        /// <param name="consumers"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterRabbitConsumer<THandler>(this ContainerBuilder builder, string consumerName, int consumers = 1)
        where THandler : class, IMessageHandler
        {
            builder.RegisterType<THandler>().AsSelf().SingleInstance();
            builder.Register(c =>
            {
                var rabbitMqHelper = c.Resolve<IRabbitHelper>();
                var messageHandler = c.Resolve<THandler>();

                rabbitMqHelper.AddConsumer(consumerName, messageHandler.HandleMessage, consumers);

                return rabbitMqHelper;
            }).As<IRabbitHelper>().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register a consumer to specific keyed IRabbitHelper.
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="builder"></param>
        /// <param name="key"></param>
        /// <param name="consumerName"></param>
        /// <param name="consumers"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterKeyedRabbitConsumer<THandler>(this ContainerBuilder builder, string key, string consumerName, int consumers = 1)
            where THandler : class, IMessageHandler
        {
            builder.RegisterType<THandler>().Keyed<THandler>(key).SingleInstance();

            builder.Register(c =>
            {
                var rabbitMqHelper = c.ResolveKeyed<IRabbitHelper>(key);
                var messageHandler = c.ResolveKeyed<THandler>(key);

                rabbitMqHelper.AddConsumer(consumerName, messageHandler.HandleMessage, consumers);

                return rabbitMqHelper;
            }).Keyed<IRabbitHelper>(key).SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register an async consumer to specific IRabbitHelper.
        /// </summary>
        /// <typeparam name="TAsyncHandler"></typeparam>
        /// <param name="builder"></param>
        /// <param name="consumerName"></param>
        /// <param name="consumers"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterAsyncRabbitConsumer<TAsyncHandler>(this ContainerBuilder builder, string consumerName, int consumers = 1)
        where TAsyncHandler : class, IAsyncMessageHandler
        {
            builder.RegisterType<TAsyncHandler>().AsSelf().SingleInstance();

            builder.Register(c =>
            {
                var rabbitMqHelper = c.Resolve<IRabbitHelper>();
                var messageHandler = c.Resolve<TAsyncHandler>();

                rabbitMqHelper.AddAsyncConsumer(consumerName, messageHandler.HandleMessageAsync, consumers);

                return rabbitMqHelper;
            }).As<IRabbitHelper>().SingleInstance();

            return builder;
        }

        /// <summary>
        /// Register an async consumer to specific keyed IRabbitHelper.
        /// </summary>
        /// <typeparam name="TAsyncHandler"></typeparam>
        /// <param name="builder"></param>
        /// <param name="key"></param>
        /// <param name="consumerName"></param>
        /// <param name="consumers"></param>
        /// <returns></returns>
        public static ContainerBuilder RegisterKeyedAsyncRabbitConsumer<TAsyncHandler>(this ContainerBuilder builder, string key, string consumerName, int consumers = 1)
            where TAsyncHandler : class, IAsyncMessageHandler
        {
            builder.RegisterType<TAsyncHandler>().Keyed<TAsyncHandler>(key).SingleInstance();

            builder.Register(c =>
            {
                var rabbitMqHelper = c.ResolveKeyed<IRabbitHelper>(key);
                var messageHandler = c.ResolveKeyed<TAsyncHandler>(key);

                rabbitMqHelper.AddAsyncConsumer(consumerName, messageHandler.HandleMessageAsync, consumers);

                return rabbitMqHelper;
            }).Keyed<IRabbitHelper>(key).SingleInstance();

            return builder;
        }

        /// <summary>
        /// Get specific IRabbitHelper service by key.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static IRabbitHelper GetRabbitHelper(this IComponentContext context, string key)
        {
            var rabbitHelper = context.ResolveKeyed<IRabbitHelper>(key);
            return rabbitHelper;
        }
    }
}
