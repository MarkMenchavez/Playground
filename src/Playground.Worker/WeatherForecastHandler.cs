using Microsoft.Extensions.Options;

using Playground.Events;
using Playground.Infrastructure;

using Rebus.Bus;
using Rebus.Handlers;
using Rebus.Pipeline;
using Rebus.Retry.Simple;

namespace Playground.Worker;

public static class HostBuilderExtensions
{
    public static IHostBuilder AddWeatherForecastHandler(this IHostBuilder builder)
    {
        builder.AddServiceBusHost<WeatherForecastHandler>(
            (builderContext, services) =>
            {
                services.AddOptions<WeatherForecastHandlerOptions>()
                    .Bind(builderContext.Configuration.GetSection(nameof(WeatherForecastHandler)));
                services.ConfigureWeatherHandlerOptions();
            },
            typeof(WeatherForecastGeneratedEvent));

        return builder;
    }

    private static void ConfigureWeatherHandlerOptions(this IServiceCollection services)
    {
        services.AddOptions<WeatherForecastHandlerOptions>()
             .Validate(o => o.ProcessDelayMilliseconds >= 1, "ProcessDelayMilliseconds must be zero or a positive integer.")
             .ValidateOnStart();
    }
}

internal static partial class WeatherForecastHandlerLogger
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{EventName} Message Received.")]
    public static partial void MessageReceived(
        this ILogger<WeatherForecastHandler> logger,
        string eventName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{EventName} Message Consumed.")]
    public static partial void MessageConsumed(
        this ILogger<WeatherForecastHandler> logger,
        string eventName);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{EventName} Message Deferred {Attempt} times.")]
    public static partial void MessageDeferred(
        this ILogger<WeatherForecastHandler> logger,
        string eventName,
        int attempt);
}

internal class WeatherForecastHandler(
    IBus bus,
    IOptions<RebusOptions> rebusOptions,
    IOptions<WeatherForecastHandlerOptions> handlerOptions,
    ILogger<WeatherForecastHandler> logger)
    : IHandleMessages<WeatherForecastGeneratedEvent>, IHandleMessages<IFailed<WeatherForecastGeneratedEvent>>
{
    private RebusOptions RebusOptions { get; } = rebusOptions.Value;

    private WeatherForecastHandlerOptions HandlerOptions { get; } = handlerOptions.Value;

    public async Task Handle(WeatherForecastGeneratedEvent message)
    {
        logger.MessageReceived(message.GetType().Name);

        await Task.Delay(HandlerOptions.ProcessDelayMilliseconds);

        var deferCount = MessageContext.Current.GetCurrentDeferCount();
        if (deferCount < RebusOptions.MaxDeferAttempts + 1 &&
            HandlerOptions.SimulateErrorOnMaxAttempts)
        {
            throw new InvalidOperationException("An error occurred while processing the weather forecast event.");
        }

        logger.MessageConsumed(message.GetType().Name);
    }

    public async Task Handle(IFailed<WeatherForecastGeneratedEvent> failed)
    {
        if (await failed.DeferAndRetryAsync(bus, RebusOptions))
        {
            logger.MessageDeferred(
                failed.Message.GetType().Name,
                MessageContext.Current.GetCurrentDeferCount() + 1);
        }
    }
}

internal class WeatherForecastHandlerOptions
{
    public int ProcessDelayMilliseconds { get; init; } = 500;

    public bool SimulateErrorOnMaxAttempts { get; init; }
}