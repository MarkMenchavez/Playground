using System.Globalization;

using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Pipeline;
using Rebus.Retry.Simple;

namespace Playground.Infrastructure;

public static class IFailedExtensions
{
    public static Task DeferAndRetryAsync<TMessage>(this IFailed<TMessage> message, IBus bus, RebusOptions options)
    {
        return RebusFailedMessageHandler.HandleAsync(
            message,
            bus,
            options);
    }
}

internal static class RebusFailedMessageHandler
{
    public static async Task HandleAsync<TMessage>(
        IFailed<TMessage> message,
        IBus bus,
        RebusOptions options)
    {
        var deferCount = 0;
        if (MessageContext.Current.Headers.TryGetValue(Rebus.Messages.Headers.DeferCount, out var currentDeferCount) &&
            int.TryParse(currentDeferCount, CultureInfo.InvariantCulture, out var parsedDeferCount))
        {
            deferCount = parsedDeferCount;
        }

        if (deferCount < options.MaxDeferAttempts)
        {
            var jitter = (Random.Shared.NextDouble() * 0.2) + 0.9;
            var delay = TimeSpan.FromSeconds(
                options.DeferDelaySeconds * (deferCount + 1) * jitter);

            await bus.Advanced.TransportMessage.Defer(delay);
            return;
        }

        throw new FailFastException(message.ErrorDescription);
    }
}