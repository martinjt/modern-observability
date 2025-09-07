using Azure.Messaging.ServiceBus;
using ModernObservability.Greeter;
using ModernObservability.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupOpenTelemetry("Greeter");
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
    GreetingsSender greetingsSender,
    ILogger<Program> logger,
    [AsParameters]Person person) => {

        if (string.IsNullOrEmpty(person.Firstname) &&
            string.IsNullOrEmpty(person.Surname))
        {
            return TypedResults.BadRequest("Please provide a firstname and surname");
        }

        var httpClient = httpClientFactory.CreateClient("agegenerator");
        var result = await httpClient.GetAsync($"profile?firstname={person.Firstname}&surname={person.Surname}");
        var response = await result.Content.ReadFromJsonAsync<AgeResponse>();

        await greetingsSender.SendMessage(person, response!.Age);

        return TypedResults.Ok($"Hi {response!.Name}, you're {response!.Age} years old");
});

app.MapHealthChecks("/healthcheck");

await app.RunAsync();

internal record Person(string Firstname, string Surname);
record AgeResponse(string Name, int Age);