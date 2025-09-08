using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace ModernObservability.Telemetry;

public static class DiagnosticSettings
{
    private readonly static string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
    private readonly static Lazy<ActivitySource> ActivitySourceInternal = new(() => new ActivitySource(AssemblyName));
    public static ActivitySource ActivitySource => ActivitySourceInternal.Value;

    internal static Lazy<Meter> Meter = new(() => new Meter(AssemblyName));
    public static Histogram<double> GreetedAge = Meter.Value.CreateHistogram<double>("greeted_age", "years", "Age of greeted person");
    public static Counter<int> EmailsSent = Meter.Value.CreateCounter<int>("emails_sent", "emails", "Number of emails sent");
}

public static class OpenTelemetryExtensions
{

    public static IServiceCollection SetupOpenTelemetry(this IServiceCollection services)
    {
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.AddOpenTelemetry()
            .UseOtlpExporter()
            .ConfigureResource(resourceBuilder => resourceBuilder.AddHostDetector())
            .WithTracing(builder => builder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddProcessor(new BaggageSpanProcessor())
                .AddSource(DiagnosticSettings.ActivitySource.Name))
            .WithMetrics(builder => builder
                .AddMeter(DiagnosticSettings.Meter.Value.Name)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        return services;
    }

    /// <summary>
    /// Extracts a value from the application properties and returns it as an array.
    /// </summary>
    /// <param name="properties"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static IEnumerable<string> ExtractContextFromApplicationProperties(this IReadOnlyDictionary<string, object> properties, string key)
    {
        var valueFromProps = properties.TryGetValue(key, out var propertyValue)
                ? propertyValue?.ToString() ?? ""
                : "";

        return [valueFromProps];
    }
}

public class BaggageSpanProcessor : BaseProcessor<Activity>
{
    public override void OnEnd(Activity activity)
    {
        foreach (var item in Baggage.Current)
            activity.SetTag(item.Key, item.Value);
    }
}