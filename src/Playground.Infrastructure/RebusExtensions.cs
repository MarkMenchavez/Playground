#pragma warning disable MA0048 // File name must match type name

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Rebus.Bus;
using Rebus.Config;
using Rebus.Serialization.Json;

namespace Playground.Infrastructure;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class;
}

internal static class RebusExtensions
{
    public static void AddOneWayBus(this IServiceCollection services, string name = "Default")
    {
        services.AddRebus(
            isDefaultBus: true,
            key: name,
            configure: (configurer, serviceProvider) =>
            {
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var webHostEnvironment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                var connectionString = configuration.GetConnectionString("RabbitMQ");

                configurer
                    .Logging(logger => logger.Serilog())
                    .Serialization(serializer => serializer.UseNewtonsoftJson(JsonInteroperabilityMode.PureJson))
                    .Transport(transport => transport
                        .UseRabbitMqAsOneWayClient(connectionString: connectionString)
                        ////.ClientConnectionName(connectionName: webHostEnvironment.ApplicationName) // Used to work in Rebus.RabbitMQ 9.4.1
                        .CustomizeConnectionFactory(connectionFactory =>
                        {
                            // Workaround -- see https://github.com/rebus-org/Rebus.RabbitMq/issues/135
                            connectionFactory.ClientProvidedName = webHostEnvironment.ApplicationName;

                            return connectionFactory;
                        }))
                    .Options(options =>
                    {
                        options.SetBusName(name);
                        options.EnableDiagnosticSources();
                    });

                return configurer;
            });
    }
}

internal class EventPublisher(IBus bus) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        return bus.Publish(message);
    }
}