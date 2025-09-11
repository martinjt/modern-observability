var builder = DistributedApplication.CreateBuilder(args);

var liveCheck = builder.AddContainer("live-check", "otel/weaver:latest")
    .WithBindMount("../../src/ModernObservability.Conventions/Model", "/conventions")
    .WithArgs("registry", "live-check", "--registry=/conventions", "--inactivity-timeout=600", "--include-unreferenced")
    .WithHttpEndpoint(name: "GRPC", targetPort: 4317);

var collector = builder.AddOpenTelemetryCollector("collector", settings => settings.ForceNonSecureReceiver = true)
    .WithConfig("./config.yaml")
    .WithAppForwarding()
    .WithEnvironment("LIVE_CHECK_ENDPOINT", liveCheck.GetEndpoint("GRPC"));

var ageGenerator = builder.AddProject<Projects.ModernObservability_AgeGenerator>("agegenerator")
    .WithHttpHealthCheck("/healthcheck", 200);

var servicebus = builder.AddAzureServiceBus("servicebus")
    .RunAsEmulator();

var smtpService = builder.AddPapercutSmtp("smtp");

var greetingsQueue = servicebus.AddServiceBusQueue("greetings");

builder.AddProject<Projects.ModernObservability_Greeter>("greeter")
    .WithReference(greetingsQueue).WaitFor(greetingsQueue)
    .WithReference(ageGenerator)
    .WithHttpHealthCheck("/healthcheck", 200);


builder.AddProject<Projects.ModernObservability_EmailSender>("emailsender")
    .WithReference(greetingsQueue).WaitFor(greetingsQueue)
    .WithReference(smtpService);

var docsGenerator = builder.AddContainer("generate-docs", "otel/weaver:latest")
    .WithBindMount("../../src/ModernObservability.Conventions/Model", "/conventions")
    .WithBindMount("../../docs", "/output")
    .WithArgs(
        "registry","generate","markdown",
        "--registry=/conventions",
        "--templates=https://github.com/open-telemetry/semantic-conventions/archive/refs/tags/v1.32.0.zip[templates]",
        "/output"
    );

builder.AddContainer("docs", "squidfunk/mkdocs-material:latest")
    .WithBindMount("../../docs", "/docs/docs")
    .WithContainerFiles("/docs", "../../docs/mkdocs.yml")
    .WithHttpEndpoint(name: "Docs", targetPort: 8000)
    .WaitForCompletion(docsGenerator);

builder.Build().Run();
