using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

namespace ModernObservability.Telemetry;

public static class OpenTelemetryExtensions
{
    public static IServiceCollection SetupOpenTelemetry(this IServiceCollection services)
    {
        services.AddOpenTelemetry()
            .UseOtlpExporter()
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddSource("ModernObservability.*"));

        return services;
    }
}
