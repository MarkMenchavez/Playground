using FluentValidation;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.FeatureManagement;

using Rebus.Config;
using Rebus.Exceptions;
using Rebus.Handlers;
using Rebus.Retry.Simple;
using Rebus.Routing.TypeBased;
using Rebus.Serialization.Json;

namespace Playground.Infrastructure;

internal static class ConnectionStringName
{
    public const string RabbitMQ = nameof(RabbitMQ);

    public const string SQLServer = nameof(SQLServer);
}