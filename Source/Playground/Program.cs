using Serilog;

Log.Logger = SerilogExtensions.CreateBootstrapLogger();

try 
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseDefaultServiceProvider(options => 
    {
        options.ValidateOnBuild = true;
        options.ValidateScopes = true;
    });

    builder.Host.UseSerilog();

    builder.WebHost.ConfigureKestrel(options => 
    {
        // TODO Configure server limits.
        options.AddServerHeader = false;
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler();
    app.UseStatusCodePages();

    app.MapGet("/", () => "Hello World!");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly.");
}
finally
{
    await Log.CloseAndFlushAsync();
}
