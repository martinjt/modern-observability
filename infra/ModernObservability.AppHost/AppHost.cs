var builder = DistributedApplication.CreateBuilder(args);

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


builder.Build().Run();
