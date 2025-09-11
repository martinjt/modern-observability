using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using Microsoft.AspNetCore.Http.Features;
using ModernObservability.Greeter;
using ModernObservability.Telemetry;
using OpenTelemetry;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupOpenTelemetry();
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureOpenTelemetryTracerProvider((sp, builder) =>
{
    var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();
    builder.SetSampler(new HealthcheckSampler(httpContextAccessor, 50));
});

builder.Services.AddServiceDiscovery();
builder.Services.ConfigureHttpClientDefaults(http => http.AddServiceDiscovery());
builder.Services.AddHealthChecks();
builder.Services.AddSingleton((sp) =>
{
    var serviceBusClient = new ServiceBusClient(sp.GetRequiredService<IConfiguration>().GetConnectionString("greetings"));
    return new GreetingsSender(serviceBusClient.CreateSender("greetings"));
});

builder.Services.AddHttpClient("agegenerator", options => options.BaseAddress = new Uri("http+https://agegenerator"));

var app = builder.Build();

app.MapGet("/", async Task<IResult>(
    IHttpClientFactory httpClientFactory,
    HttpContext context,
    GreetingsSender greetingsSender,
    ILogger<Program> logger,
    [AsParameters]Person person) => {


        using (var activity = DiagnosticSettings.ActivitySource.StartActivity("dummy"))
        {

            Activity.Current?.SetTag("firstname", person.Firstname);
            Activity.Current?.SetTag("surname", person.Surname);

            activity?.SetTag("firstname", person.Firstname);
            activity?.SetTag("surname", person.Surname);

            var httpActivityFeature = context.Features.Get<IHttpActivityFeature>();

            httpActivityFeature?.Activity?.SetTag("firstname", person.Firstname);
            httpActivityFeature?.Activity?.SetTag("surname", person.Surname);

            Baggage.SetBaggage("user_agent_original", context.Request.Headers.UserAgent.ToString());

            var httpClient = httpClientFactory.CreateClient("agegenerator");
            var result = await httpClient.GetAsync($"profile?firstname={person.Firstname}&surname={person.Surname}");
            var response = await result.Content.ReadFromJsonAsync<AgeResponse>();

            await greetingsSender.SendMessage(person, response!.Age);

            return TypedResults.Ok($"Hi {response!.Name}, you're {response!.Age} years old");
        }
});

app.MapHealthChecks("/healthcheck");

await app.RunAsync();

internal record Person(string Firstname, string Surname);
record AgeResponse(string Name, int Age);