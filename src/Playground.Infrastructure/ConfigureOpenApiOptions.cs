using Asp.Versioning.ApiExplorer;

using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Options;

namespace Playground.Infrastructure;

internal class ConfigureOpenApiOptions(IApiVersionDescriptionProvider versionDescriptionProvider)
    : IConfigureNamedOptions<OpenApiOptions>
{
    public void Configure(string? name, OpenApiOptions options)
    {
        var description = versionDescriptionProvider.ApiVersionDescriptions
            .FirstOrDefault(d => string.Equals(d.GroupName, name, StringComparison.OrdinalIgnoreCase));
        if (description is not null)
        {
            options.ShouldInclude = (api) => string.Equals(api.GroupName, description.GroupName, StringComparison.OrdinalIgnoreCase);
        }
    }

    public void Configure(OpenApiOptions options) => Configure(Options.DefaultName, options);
}