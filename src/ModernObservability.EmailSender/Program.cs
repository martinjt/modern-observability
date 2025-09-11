using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ModernObservability.EmailSender;
using ModernObservability.Telemetry;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddOpenTelemetry()
            .WithTracing(tracing => tracing.AddSource(MailKit.Telemetry.SmtpClient.ActivitySourceName));

        services.SetupOpenTelemetry();
        // services.AddSingleton((sp) =>
        // {
        //     var serviceBusClient = new ServiceBusClient(sp.GetRequiredService<IConfiguration>().GetConnectionString("greetings"));
        //     return serviceBusClient.CreateProcessor("greetings");
        // });
        // services.AddHostedService<MessageProcessorService>();
        // Uncomment this to enable batch processing, comment out the one above when you do
        services.AddSingleton((sp) =>
        {
            var serviceBusClient = new ServiceBusClient(sp.GetRequiredService<IConfiguration>().GetConnectionString("greetings"));
            return serviceBusClient.CreateReceiver("greetings", new ServiceBusReceiverOptions
            {
                ReceiveMode = ServiceBusReceiveMode.ReceiveAndDelete,
            });
        });
        services.AddHostedService<BatchMessageProcessorService>();
        services.AddSingleton<SMTPSender>();
    })
    .Build();

await host.RunAsync();

