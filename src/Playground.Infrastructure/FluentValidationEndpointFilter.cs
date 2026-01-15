using FluentValidation;

using Microsoft.AspNetCore.Http;

namespace Playground.Infrastructure;

public sealed class FluentValidationEndpointFilter<T>(IValidator<T> validator) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var model = context.Arguments.OfType<T>().FirstOrDefault();
        if (model is null)
        {
            return await next(context).ConfigureAwait(false);
        }

        var result = await validator.ValidateAsync(model, context.HttpContext.RequestAborted).ConfigureAwait(false);

        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray(),
                    StringComparer.OrdinalIgnoreCase);

            return TypedResults.ValidationProblem(
                errors,
                title: "One or more validation errors occurred");
        }

        return await next(context).ConfigureAwait(false);
    }
}