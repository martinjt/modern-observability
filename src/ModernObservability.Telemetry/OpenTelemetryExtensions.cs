using System.Diagnostics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ModernObservability.Telemetry;

public static class DiagnosticSettings
{
    private readonly static Lazy<ActivitySource> ActivitySourceInternal = new(() => new ActivitySource(Assembly.GetExecutingAssembly().GetName().Name!));
    public static ActivitySource ActivitySource = ActivitySourceInternal.Value;
}

public static class OpenTelemetryExtensions
{
    
    public static IServiceCollection SetupOpenTelemetry(this IServiceCollection services)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.AddOpenTelemetry()
            .UseOtlpExporter()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSource(DiagnosticSettings.ActivitySource.Name));

        return services;
    }
}
