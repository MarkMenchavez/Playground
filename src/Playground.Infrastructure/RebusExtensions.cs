using FluentValidation;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;

namespace Playground.Infrastructure;

public static class RebusExtensions
{
    internal const string DefaultTenant = "Global";

    public static void AddServiceBusHost<THandler>(
        this IHostBuilder builder,
        Action<HostBuilderContext, IServiceCollection> configure,
        params Type[] subscriptions)
        where THandler : IHandleMessages
    {
        builder.AddRebusService(
            (builderContext, services) =>
            {
                EnsureConfiguration(builderContext.Configuration);

                var handlerType = typeof(THandler);
                var handlerName = handlerType.Name;

                var configuration = builderContext.Configuration
                    .GetSection($"{RebusOptions.SectionName}:{handlerName}");

                var options = new RebusOptions();
                configuration.Bind(options);

                EnsureRebusOptions(options, handlerName);

                services.Configure<RebusOptions>(configuration);

                services.AddSingleton(TimeProvider.System);
                services.AddFeatureManagement();
                services.AddValidatorsFromAssemblyContaining<THandler>();
                services.AddServiceDiscovery();
                services.ConfigureHeaderPropagation(builderContext.Configuration);
                services.UseMinimalHttpLogger();
                services.TryAddTransient<IEventPublisher, EventPublisher>();

                configure(builderContext, services);

                services.AddRebusHandler(handlerType);

                var tenants = new HashSet<string>(options.Tenants, StringComparer.OrdinalIgnoreCase)
                {
                    DefaultTenant
                };

                foreach (var tenant in tenants)
                {
                    services.AddFullServiceBus(handlerName, tenant, options, subscriptions);
                }
            },
            typeof(IHostApplicationLifetime),
            typeof(ILoggerFactory),
            typeof(IConfiguration),
            typeof(IWebHostEnvironment));
    }

    internal static void AddOneWayServiceBus(this IServiceCollection services, string name = "Default")
    {
        services.AddRebus(
            isDefaultBus: true,
            key: name,
            configure: (configurer, serviceProvider) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var connectionString = configuration.GetConnectionString(ConnectionStringName.RabbitMQ);

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new RebusConfigurationException("No RabbitMQ connection string provided.");
                }

                configurer
                    .Logging(logger => logger.Serilog())
                    .Serialization(serializer => serializer.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
                    .Transport(transport => ConfigureTransport(transport, connectionString, webHostEnvironment))
                    .Options(options => ConfigureRebusOptions(options, name));

                return configurer;
            });
    }

    private static void EnsureConfiguration(IConfiguration configuration)
    {
        var rabbitMQConnectionString = configuration.GetConnectionString(ConnectionStringName.RabbitMQ);
        if (string.IsNullOrWhiteSpace(rabbitMQConnectionString))
        {
            throw new RebusConfigurationException("No RabbitMQ connection string provided.");
        }

        var sqlServerConnectionString = configuration.GetConnectionString(ConnectionStringName.SQLServer);
        if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
        {
            throw new RebusConfigurationException("No SQLServer connection string provided.");
        }
    }

    private static void EnsureRebusOptions(RebusOptions options, string handlerName)
    {
        if (string.IsNullOrWhiteSpace(options.InputQueueName))
        {
            throw new RebusConfigurationException($"InputQueueName is required for {handlerName}.");
        }

        if (options.NumberOfWorkers < 0)
        {
            throw new RebusConfigurationException("NumberOfWorkers must be >= 0.");
        }

        if (options.MaxParallelism <= 0)
        {
            throw new RebusConfigurationException("MaxParallelism must be > 0.");
        }

        if (options.MaxDeferAttempts < 0)
        {
            throw new RebusConfigurationException("MaxDeferAttempts must be >= 0.");
        }

        if (options.DeferDelaySeconds <= 0)
        {
            throw new RebusConfigurationException("DeferDelaySeconds must be > 0.");
        }

        if (options.SecondLevelRetriesEnabled &&
            options.MaxDeliveryAttempts > 1)
        {
            throw new RebusConfigurationException($"MaxDeliverAttemps must be 1 when using SecondLevelRetries for {handlerName}.");
        }
    }

    private static void AddFullServiceBus(
        this IServiceCollection services,
        string handlerName,
        string tenant,
        RebusOptions rebusOptions,
        params Type[] subscriptions)
    {
        services.AddRebus(
            isDefaultBus: false,
            key: $"{handlerName}_{tenant}",
            onCreated: async bus =>
            {
                foreach (var subscription in subscriptions)
                {
                    await bus.Subscribe(subscription);
                }
            },
            configure: (configurer, serviceProvider) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();

                var rabbitMQConnectionString = configuration.GetConnectionString(ConnectionStringName.RabbitMQ)!;
                var sqlServerConnectionString = configuration.GetConnectionString(ConnectionStringName.SQLServer)!;

                configurer
                    .Logging(logger => logger.Serilog())
                    .Serialization(serializer => serializer.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
                    .Transport(transport => ConfigureTransport(transport, rabbitMQConnectionString, tenant, rebusOptions, webHostEnvironment))
                    .Timeouts(timeoutManager => ConfigureTimeoutManager(timeoutManager, sqlServerConnectionString, tenant, rebusOptions))
                    .Routing(router => ConfigureRebusRouting(router, rebusOptions))
                    .Options(options => ConfigureRebusOptions(options, handlerName, tenant, rebusOptions));

                return configurer;
            });
    }

    private static void ConfigureTransport(
        StandardConfigurer<Rebus.Transport.ITransport> transport,
        string connectionString,
        IWebHostEnvironment webHostEnvironment)
    {
        transport
            .UseRabbitMqAsOneWayClient(connectionString: connectionString)
            .SetConnectionName(webHostEnvironment.ApplicationName);
    }

    private static void ConfigureTransport(
        StandardConfigurer<Rebus.Transport.ITransport> transport,
        string connectionString,
        string tenant,
        RebusOptions rebusOptions,
        IWebHostEnvironment webHostEnvironment)
    {
        transport
            .UseRabbitMq(
                connectionString: connectionString,
                inputQueueName: $"{rebusOptions.InputQueueName}_{tenant}")
            .ExchangeNames(topicExchangeName: tenant)
            .SetConnectionName(webHostEnvironment.ApplicationName);
    }

    private static void ConfigureTimeoutManager(
        StandardConfigurer<Rebus.Timeouts.ITimeoutManager> timeoutManager,
        string connectionString,
        string tenant,
        RebusOptions rebusOptions)
    {
        timeoutManager.StoreInSqlServer(
            connectionString: connectionString,
            tableName: $"{rebusOptions.InputQueueName}_{tenant}_Timeout");
    }

    private static void ConfigureRebusRouting(StandardConfigurer<Rebus.Routing.IRouter> router, RebusOptions rebusOptions)
    {
        var builder = router.TypeBased();
        foreach (var mapping in rebusOptions.TypeBasedMaps)
        {
            var type = Type.GetType(mapping.TypeName);
            if (type == null)
            {
                throw new RebusConfigurationException($"Type {mapping.TypeName} could not be resolved.");
            }

            builder.Map(type, mapping.DestinationAddress);
        }
    }

    private static void ConfigureRebusOptions(OptionsConfigurer options, string name)
    {
        options.SetBusName(name);
        options.EnableDiagnosticSources();
    }

    private static void ConfigureRebusOptions(OptionsConfigurer options, string handlerName, string tenant, RebusOptions rebusOptions)
    {
        options.SetBusName($"{handlerName}_{tenant}");
        options.SetNumberOfWorkers(rebusOptions.NumberOfWorkers);
        options.SetMaxParallelism(rebusOptions.MaxParallelism);

        // IMPORTANT:
        // We use IFailed<T> to orchestrate retries manually.
        // Therefore:
        // - MaxDeliveryAttempts MUST be 1
        // - SecondLevelRetries MUST be enabled
        options.RetryStrategy(
            errorQueueName: $"{rebusOptions.InputQueueName}_{tenant}_Error",
            maxDeliveryAttempts: rebusOptions.MaxDeliveryAttempts,
            secondLevelRetriesEnabled: rebusOptions.SecondLevelRetriesEnabled);

        options.EnableDiagnosticSources();
    }
}