using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace ModernObservability.EmailSender;

class SMTPSender(IConfiguration configuration, ILogger<SMTPSender> logger)
{
    private readonly SmtpClient _smtpClient = new();
    private string _smtpConnectionString = configuration.GetConnectionString("smtp")!;

    public void SendEmails(IEnumerable<GreetedMessage> greetedMessages)
    {
        lock (_smtpClient)
        {
            var messages = new List<MimeMessage>();
            foreach (var greetedMessage in greetedMessages)
            {
                logger.LogInformation("Sending email to {Firstname} {Surname}", greetedMessage.Firstname, greetedMessage.Surname);

                var message = new MimeMessage();
                var from = new MailboxAddress("Admin", "admin@modern-observability.workshop");
                message.From.Add(from);
                var to = new MailboxAddress(greetedMessage.Firstname, $"{greetedMessage.Firstname}@{greetedMessage.Surname}.com");
                message.To.Add(to);
                message.Subject = $"Welcome to Modern Observability {greetedMessage.Firstname}";

                var bb = new BodyBuilder
                {
                    TextBody = "There is a world beyond auto-instrumentation"
                };
                messages.Add(message);
            }

            _smtpClient.Connect(new Uri(_smtpConnectionString[9..]));
            foreach (var message in messages)
            {
                _smtpClient.Send(message);
            }
            _smtpClient.Disconnect(true);
        }
    }
}