using System.Diagnostics;
using ModernObservability.Telemetry;
using OpenTelemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.SetupOpenTelemetry("AgeGenerator");
builder.Services.AddHealthChecks();

var app = builder.Build();

var randomAge = new Random();

// Configure the HTTP request pipeline.
app.MapGet("/profile", (string firstname, string surname) =>
{
    var userAgent = Baggage.Current.GetBaggage("original_user_agent");
    Activity.Current?.SetTag("original_user_agent", userAgent);

    var age = randomAge.Next(18, 100);
    return new AgeResponse($"{firstname} {surname}", age);
});

app.MapHealthChecks("/healthcheck");

await app.RunAsync();

record AgeResponse(string Name, int Age);