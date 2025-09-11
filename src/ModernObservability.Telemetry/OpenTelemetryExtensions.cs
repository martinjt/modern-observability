using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Diagnostics.Tracing;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
                .AddSource(DiagnosticSettings.ActivitySource.Name)
                .AddSource("Azure.Messaging.ServiceBus.*"))
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

public class LoggingOpenTelemetryListener : EventListener
{
    private readonly ILogger<LoggingOpenTelemetryListener> _logger;

    public LoggingOpenTelemetryListener(ILogger<LoggingOpenTelemetryListener> logger)
    {
        _logger = logger;
    }
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        if (eventSource.Name.StartsWith("OpenTelemetry"))
            EnableEvents(eventSource, EventLevel.Error);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        _logger.LogWarning(eventData.Message, eventData.Payload?.Select(p => p?.ToString())?.ToArray());
    }
}

public class HealthcheckSampler(IHttpContextAccessor httpContextAccessor, int percentage) : Sampler
{
    private readonly Random _random = new Random();
    public override SamplingResult ShouldSample(in SamplingParameters samplingParameters)
    {
        if (httpContextAccessor.HttpContext?.Request.Path.StartsWithSegments("/healthcheck") ?? false &&
            _random.Next(1, 100) > percentage)
            return new SamplingResult(SamplingDecision.Drop);

        return new SamplingResult(SamplingDecision.RecordAndSample, [
            new("sample.rate", percentage),
            new("sample.reason", "healthcheck-endpoint"),
            new("sampler.name", nameof(HealthcheckSampler))
        ], $"ot:sr={percentage}");
    }
}