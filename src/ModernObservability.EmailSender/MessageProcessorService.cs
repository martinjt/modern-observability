using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModernObservability.EmailSender;
using ModernObservability.Telemetry;
using OpenTelemetry.Context.Propagation;

record GreetedMessage(string Firstname, string Surname, int Age);

/// <summary>
/// Single Message Processor, runs each message synchronously
/// </summary>
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
        _logger.LogInformation("Received message: {MessageId}, {Message}", args.Message.MessageId, args.Message.Body.ToString());
        if (message is null)
        {
            await args.CompleteMessageAsync(args.Message);
            return;
        }
        _smtpSender.SendEmails([message]);
    }

    static Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        Console.WriteLine($"Error: {args.Exception.Message}");
        return Task.CompletedTask;
    }
}

/// <summary>
/// Batch Message Processor, gets up to 10 messages at a time and processes them in batches
/// </summary>
class BatchMessageProcessorService(ServiceBusReceiver _serviceBusReceiver, SMTPSender _smtpSender, ILogger<MessageProcessorService> _logger) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _serviceBusReceiver!.ReceiveMessagesAsync(10, TimeSpan.FromSeconds(10), stoppingToken);
            var receivedGreetings = messages
                .Select(m => JsonSerializer.Deserialize<GreetedMessage>(m.Body)!)
                .ToList();

            _smtpSender.SendEmails(receivedGreetings);
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
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
