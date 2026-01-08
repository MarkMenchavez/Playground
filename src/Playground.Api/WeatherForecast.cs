using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Playground.Api;

internal interface IWeatherForecastService
{
    Task<IEnumerable<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<WeatherForecast>> GetForecastsAsync(int days, CancellationToken cancellationToken = default);
}

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeatherForecast(this IServiceCollection services, Action<WeatherForecastServiceOptions> configureOptions)
    {
        services.AddOptions<WeatherForecastServiceOptions>()
            .Configure(configureOptions);

        services.AddWeatherForecast();

        return services;
    }

    public static IServiceCollection AddWeatherForecast(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WeatherForecastServiceOptions>()
            .Bind(configuration.GetSection(nameof(WeatherForecastService)));

        services.AddWeatherForecast();

        return services;
    }

    private static IServiceCollection AddWeatherForecast(this IServiceCollection services)
    {
        services.TryAddTransient<IWeatherForecastService, WeatherForecastService>();
        services.ConfigureWeatherForecastServiceOptions();

        return services;
    }

    private static OptionsBuilder<WeatherForecastServiceOptions> ConfigureWeatherForecastServiceOptions(this IServiceCollection services)
    {
        return services.AddOptions<WeatherForecastServiceOptions>()
             .Validate(o => o.DefaultDays > 0, "DefaultDays must be greater than zero.")
             .Validate(o => o.Summaries.Length > 0, "Summaries must contain at least one summary.")
             .Validate(o => o.GenerationDelayMilliseconds >= 0, "GenerationDelayMilliseconds must be zero or a positive integer.")
             .Validate(o => o.MinimumTemperatureCelsius < o.MaximumTemperatureCelsius, "MinimumTemperatureCelsius must be less than MaximumTemperatureCelsius.")
             .ValidateOnStart();
    }
}

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWeatherForecast(this IEndpointRouteBuilder app)
    {
        app.MapGet("/weatherforecast", async (IWeatherForecastService service, CancellationToken cancellationToken)
                => TypedResults.Ok(await service.GetForecastsAsync(cancellationToken)))
            .WithName("GetWeatherForecast")
            .AddOpenApiOperationTransformer((operation, context, cancelalationToken) =>
            {
                operation.Summary = "Gets the weather forecast for the default number of days.";
                operation.Description = "Retrieves an array of weather forecast data for the default number of days configured in the service.";

                return Task.CompletedTask;
            });

        return app;
    }
}

internal static partial class WeatherForecastLogger
{
    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "{Count} weather forecast generated.")]
    public static partial void WeatherForecastGenerated(
        this ILogger<WeatherForecastService> logger,
        int count);
}

internal class WeatherForecastServiceOptions
{
    public int DefaultDays { get; set; }

    public string[] Summaries { get; set; } = [];

    public int GenerationDelayMilliseconds { get; set; } = 10;

    public int MinimumTemperatureCelsius { get; set; } = -20;

    public int MaximumTemperatureCelsius { get; set; } = 55;
}

internal class WeatherForecastService(
    IOptions<WeatherForecastServiceOptions> options,
    ILogger<WeatherForecastService> logger)
    : IWeatherForecastService
{
    private readonly WeatherForecastServiceOptions serviceOptions = options.Value;

    public Task<IEnumerable<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default)
    {
        return GetForecastsAsync(serviceOptions.DefaultDays, cancellationToken);
    }

    public async Task<IEnumerable<WeatherForecast>> GetForecastsAsync(int days, CancellationToken cancellationToken = default)
    {
        if (days <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(days), "Days must be greater than zero.");
        }

        var summaries = serviceOptions.Summaries;

        var forecasts = new List<WeatherForecast>();

        foreach (var index in Enumerable.Range(1, days))
        {
            await Task.Delay(serviceOptions.GenerationDelayMilliseconds, cancellationToken);
            forecasts.Add(new WeatherForecast(
                DateOnly.FromDateTime(DateTime.Now.AddDays(index - 1)),
                Random.Shared.Next(serviceOptions.MinimumTemperatureCelsius, serviceOptions.MaximumTemperatureCelsius),
                summaries[Random.Shared.Next(summaries.Length)]));
        }

        logger.WeatherForecastGenerated(forecasts.Count);

        return forecasts;
    }
}