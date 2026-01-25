using System.Globalization;

using Rebus.Bus;
using Rebus.Exceptions;
using Rebus.Pipeline;
using Rebus.Retry.Simple;

namespace Playground.Infrastructure;

public static class IFailedExtensions
{
    public static Task<bool> DeferAndRetryAsync<TMessage>(
        this IFailed<TMessage> message,
        IBus bus,
        RebusOptions options)
    {
        return RebusFailedMessageHandler.HandleAsync(
            message,
            bus,
            options);
    }
}

public static class IMessageContextExtensions
{
    public static int GetCurrentDeferCount(this IMessageContext context)
    {
        var deferCount = 0;
        if (context.Headers.TryGetValue(Rebus.Messages.Headers.DeferCount, out var currentDeferCount) &&
            int.TryParse(currentDeferCount, CultureInfo.InvariantCulture, out var parsedDeferCount))
        {
            deferCount = parsedDeferCount;
        }

        return deferCount;
    }
}

internal static class RebusFailedMessageHandler
{
    internal static async Task<bool> HandleAsync<TMessage>(
        IFailed<TMessage> message,
        IBus bus,
        RebusOptions options)
    {
        var context = MessageContext.Current;
        if (context == null)
        {
            return false;
        }

        var deferCount = context.GetCurrentDeferCount();

        if (deferCount >= options.MaxDeferAttempts)
        {
            throw new FailFastException(message.ErrorDescription);
        }

        var delay = CalculateDelay(deferCount, options);

        await bus.Advanced.TransportMessage.Defer(delay);

        return true;
    }

    private static TimeSpan CalculateDelay(int deferCount, RebusOptions options)
    {
        var minJitter = Math.Min(options.JitterMin, options.JitterMax);
        var maxJitter = Math.Max(options.JitterMin, options.JitterMax);
        var jitter = options.UseDefaultJitter
            ? (Random.Shared.NextDouble() * (maxJitter - minJitter)) + minJitter
            : 1;
        jitter = Math.Max(0, jitter);

        var effectiveDeferCount = options.IgnoreDeferCountForBackoff ? 0 : deferCount;
        var safeBackoff = Math.Clamp(options.RetryBackoffFactor, 0.0, 10.0);
        var baseDelay = Math.Max(1, options.DeferDelaySeconds);
        var delaySeconds = options.UseFixedDelay
            ? baseDelay * jitter
            : baseDelay * Math.Pow(safeBackoff, effectiveDeferCount) * jitter;

        var minDelay = options.MinDeferDelaySeconds > 0 ? options.MinDeferDelaySeconds : 0;
        var maxDelay = options.MaxDeferDelaySeconds > 0 ? options.MaxDeferDelaySeconds : 3600;
        delaySeconds = Math.Clamp(delaySeconds, minDelay, maxDelay);

        return TimeSpan.FromSeconds(delaySeconds);
    }
}