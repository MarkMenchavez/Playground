using Rebus.Bus;
using Rebus.Extensions;

namespace Playground.Infrastructure;

public interface IEventPublisher
{
    Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class;
}

internal class EventPublisher(IBus bus) : IEventPublisher
{
    public Task PublishAsync<T>(T message, CancellationToken cancellationToken)
        where T : class
    {
        var tenant = RebusExtensions.DefaultTenant;
        var topic = $"{message.GetType().GetSimpleAssemblyQualifiedName()}@{tenant}";

        return bus.Advanced.Topics.Publish(topic, message);
    }
}