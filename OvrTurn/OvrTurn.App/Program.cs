using OvrTurn.App.Components;
using OvrTurn.App.Security;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/ovrturn-app-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/ovrturn-app-.log", rollingInterval: RollingInterval.Day));

    // Add services to the container.
    builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

    builder.Services.AddProblemDetails();

    // Typed HttpClient pointed at the OvrTurn API.
    // Base URL comes from User Secrets (dev) / Key Vault (prod) — never from appsettings.json.
    var apiBaseUrl = builder.Configuration["OvrTurnApi:BaseUrl"];

    if (string.IsNullOrWhiteSpace(apiBaseUrl))
    {
        throw new InvalidOperationException(
            "OvrTurnApi:BaseUrl is not configured. Set it via User Secrets or environment variables.");
    }

    builder.Services.AddHttpClient("OvrTurnApi", client =>
    {
        client.BaseAddress = new Uri(apiBaseUrl);
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        // HSTS tells browsers to only ever contact this site over HTTPS for the next year.
        // Only enabled outside Development — HSTS in dev would lock localhost into HTTPS.
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();

    // Security headers must run before anything that writes a response.
    app.UseSecurityHeaders();

    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OvrTurn.App terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}



