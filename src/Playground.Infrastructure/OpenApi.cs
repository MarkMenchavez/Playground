using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using Scalar.AspNetCore;

namespace Playground.Infrastructure;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddVersionedOpenApi(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = ApiVersion.Default;
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

        services.ConfigureOptions<ConfigureOpenApiOptions>();
        services.AddOpenApi("v1");
        services.AddOpenApi("v2");
        services.AddOpenApi("v3");

        return services;
    }

    public static WebApplication MapScalarOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                var sorted = provider.ApiVersionDescriptions
                    .OrderByDescending(d => d.ApiVersion.MajorVersion)
                    .ThenByDescending(d => d.ApiVersion.MinorVersion)
                    .ToList();

                foreach (var description in sorted)
                {
                    options.AddDocument(description.GroupName);
                }
            });
        }

        return app;
    }
}

internal class ConfigureOpenApiOptions(IApiVersionDescriptionProvider versionDescriptionProvider)
    : IConfigureNamedOptions<OpenApiOptions>
{
    public void Configure(string? name, OpenApiOptions options)
    {
        var description = versionDescriptionProvider.ApiVersionDescriptions
            .FirstOrDefault(d => d.GroupName == name);
        if (description is not null)
        {
            options.ShouldInclude = (api) => api.GroupName == name;
        }
    }

    public void Configure(OpenApiOptions options) => Configure(Options.DefaultName, options);
}