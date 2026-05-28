using Asp.Versioning;

using FluentValidation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

using Playground.Events;
using Playground.Infrastructure;

namespace Playground.Api;

internal interface IWeatherForecastService
{
    Task<IEnumerable<WeatherForecast>> GetForecastsAsync(CancellationToken cancellationToken = default);

    Task<IEnumerable<WeatherForecast>> GetForecastsAsync(int days, CancellationToken cancellationToken = default);
}

internal record GetForecastRequest([property: FromQuery(Name = "days")] int Days) : IParsable<GetForecastRequest>
{
    public static bool TryParse(string? s, IFormatProvider? provider, out GetForecastRequest result)
    {
        if (int.TryParse(s, provider, out var days))
        {
            result = new GetForecastRequest(days);
            return true;
        }

        result = default!;
        return false;
    }

    public static GetForecastRequest Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
        {
            return result;
        }

        throw new FormatException($"Invalid GetForecastRequest: {s}");
    }
}

internal class GetForecastRequestValidator : AbstractValidator<GetForecastRequest>
{
    public GetForecastRequestValidator()
    {
        RuleFor(x => x.Days).GreaterThan(0).WithMessage("Days must be greater than zero");
    }
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

    private static void AddWeatherForecast(this IServiceCollection services)
    {
        services.AddScoped<IValidator<GetForecastRequest>, GetForecastRequestValidator>();

        services.AddEchoServiceAgent();
        services.TryAddTransient<IWeatherForecastService, WeatherForecastService>();
        services.ConfigureWeatherForecastServiceOptions();
    }

    private static void ConfigureWeatherForecastServiceOptions(this IServiceCollection services)
    {
        services.AddOptions<WeatherForecastServiceOptions>()
             .Validate(o => o.DefaultDays > 0, "DefaultDays must be greater than zero.")
             .Validate(o => o.Summaries.Length > 0, "Summaries must contain at least one summary.")
             .Validate(o => o.GenerationMaxSeconds > 0, "GenerationMaxSecond must be greater than zero.")
             .Validate(o => o.GenerationDelayMilliseconds >= 0, "GenerationDelayMilliseconds must be zero or a positive integer.")
             .Validate(o => o.MinimumTemperatureCelsius < o.MaximumTemperatureCelsius, "MinimumTemperatureCelsius must be less than MaximumTemperatureCelsius.")
             .ValidateOnStart();
    }
}

internal static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapWeatherForecast(this IEndpointRouteBuilder app)
    {
        var versionSet = app.NewApiVersionSet()
            .HasDeprecatedApiVersion(new ApiVersion(1, 0))
            .HasApiVersion(new ApiVersion(2, 0))
            .ReportApiVersions()
            .Build();

        var groupV1 = app.MapGroup("/api/v{version:apiversion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(1.0);

        groupV1.MapGet("weatherforecast", async (IWeatherForecastService service, CancellationToken cancellationToken)
                => TypedResults.Ok(await service.GetForecastsAsync(cancellationToken)))
            .MapToApiVersion(1.0)
            .AddOpenApiOperationTransformer((operation, context, cancellationToken) =>
            {
                operation.Deprecated = true;
                operation.Summary = "Gets the weather forecast for the default number of days.";
                operation.Description = "Retrieves an array of weather forecast data for the default number of days configured in the service.";

                return Task.CompletedTask;
            });

        var groupV2 = app.MapGroup("/api/v{version:apiversion}")
            .WithApiVersionSet(versionSet)
            .HasApiVersion(2.0);

        groupV2.MapGet("weatherforecast", async ([FromQuery]GetForecastRequest days, IWeatherForecastService service, CancellationToken cancellationToken) =>
                TypedResults.Ok(await service.GetForecastsAsync(days.Days, cancellationToken)))
            .AddEndpointFilter<FluentValidationEndpointFilter<GetForecastRequest>>()
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .Produces<IEnumerable<WeatherForecast>>(StatusCodes.Status200OK)
            .AddOpenApiOperationTransformer((op, ctx, ct) =>
            {
                op.Summary = "Gets weather forecast for specified days";
                op.Description = "Validates input and returns forecasts";
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
    public int DefaultDays { get; init; } = 7;

    public string[] Summaries { get; init; } = [];

    public int GenerationMaxSeconds { get; init; } = 2;

    public int GenerationDelayMilliseconds { get; init; } = 10;

    public int MinimumTemperatureCelsius { get; init; } = -20;

    public int MaximumTemperatureCelsius { get; init; } = 55;
}

internal class WeatherForecastService(
    IEchoServiceAgent echoServiceAgent,
    IEventPublisher eventPublisher,
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
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(serviceOptions.GenerationMaxSeconds));
        using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        await echoServiceAgent.EchoAsync($"Generating {days} weather forecasts.", linkedTokenSource.Token);

        var summaries = serviceOptions.Summaries;

        var forecasts = new List<WeatherForecast>();

        foreach (var index in Enumerable.Range(1, days))
        {
            await Task.Delay(serviceOptions.GenerationDelayMilliseconds, linkedTokenSource.Token);

            var forecast = new WeatherForecast(
                DateOnly.FromDateTime(DateTime.UtcNow.AddDays(index - 1)),
                Random.Shared.Next(serviceOptions.MinimumTemperatureCelsius, serviceOptions.MaximumTemperatureCelsius),
                summaries[Random.Shared.Next(summaries.Length)]);

            await eventPublisher.PublishAsync(new WeatherForecastGeneratedEvent(), linkedTokenSource.Token);

            forecasts.Add(forecast);
        }

        logger.WeatherForecastGenerated(forecasts.Count);

        return forecasts;
    }
}