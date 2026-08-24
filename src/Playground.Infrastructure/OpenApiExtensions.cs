using Asp.Versioning;
using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Scalar.AspNetCore;

namespace Playground.Infrastructure;

internal static class OpenApiExtensions
{
    public static IServiceCollection AddVersionedOpenApi(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.ReportApiVersions = true;
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
            options.ApiVersionParameterSource = new UrlSegmentApiVersionReader();
        }).AddOpenApi();

        services.ConfigureOptions<ConfigureOpenApiOptions>();

        return services;
    }

    public static WebApplication MapScalarOpenApi(this WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi().WithDocumentPerVersion();

            app.MapScalarApiReference(options =>
            {
                var descriptions = app.DescribeApiVersions()
                    .OrderByDescending(d => d.ApiVersion.MajorVersion)
                    .ThenByDescending(d => d.ApiVersion.MinorVersion)
                    .ToList();

                foreach (var groupName in descriptions.Select(description => description.GroupName))
                {
                    options.AddDocument(groupName, groupName);
                }
            });
        }

        return app;
    }
}