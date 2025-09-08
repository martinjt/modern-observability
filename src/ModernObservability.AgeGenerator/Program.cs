using System.Diagnostics;
using ModernObservability.Telemetry;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupOpenTelemetry();

builder.Services.AddHealthChecks();

var app = builder.Build();

var randomAge = new Random();

// Configure the HTTP request pipeline.
app.MapGet("/profile", (string firstname, string surname) =>
{
    var userAgent = Baggage.Current.GetBaggage("user_agent_original");

    Activity.Current?.SetTag("user_agent_original", userAgent);

    var age = randomAge.Next(18, 100);

    DiagnosticSettings.GreetedAge.Record(age, new("firstname", firstname), new("surname", surname));

    return new AgeResponse($"{firstname} {surname}", age);
});

app.MapHealthChecks("/healthcheck");

await app.RunAsync();

record AgeResponse(string Name, int Age);