using Serilog;
using Serilog.Extensions.Hosting;

namespace Playground.Infrastructure;

public static class SerilogExtensions
{
    public static ReloadableLogger CreateBootstrapLogger()
    {
        return new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();
    }
}