using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModernObservability.EmailSender;

AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton((sp) =>
        {
            var serviceBusClient = new ServiceBusClient(sp.GetRequiredService<IConfiguration>().GetConnectionString("greetings"));
            return serviceBusClient.CreateProcessor("greetings");
        });
        services.AddSingleton<SMTPSender>();
        services.AddHostedService<MessageProcessorService>();
    })
    .Build();

await host.RunAsync();

class MessageProcessorService(ServiceBusProcessor _serviceBusProcessor, SMTPSender _smtpSender, ILogger<MessageProcessorService> _logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _serviceBusProcessor.ProcessMessageAsync += ProcessMessageAsync;
        _serviceBusProcessor.ProcessErrorAsync += ProcessErrorAsync;
        await _serviceBusProcessor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Started processing messages");
    }

    async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var message = args.Message<GreetedMessage>();
        _logger.LogInformation("Received message: {MessageId}, {Message}", args.Message.MessageId,args.Message.Body.ToString());
        if (message is null)
        {
            await args.CompleteMessageAsync(args.Message);
            return;
        }
        _smtpSender.SendEmails(message);
    }

    static Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}

static class ServiceBusMessageExtensions
{
    public static T? Message<T>(this ProcessMessageEventArgs processMessageEventArgs)
    {
        if (processMessageEventArgs?.Message?.Body is null)
        {
            throw new InvalidOperationException("Message body is null");
        }
        return JsonSerializer.Deserialize<T>(processMessageEventArgs.Message.Body);
    }
}
record GreetedMessage(string Firstname, string Surname, int Age);
