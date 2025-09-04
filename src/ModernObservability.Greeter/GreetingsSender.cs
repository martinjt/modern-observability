using System.Text.Json;
using Azure.Messaging.ServiceBus;

namespace ModernObservability.Greeter;

internal class GreetingsSender(ServiceBusSender _sender)
{
    public async Task SendMessage(Person person, int age)
    {
        await _sender.SendMessageAsync(new JsonServiceBusMessage(new GreetedMessage(person.Firstname, person.Surname, age)));
    }
}

internal class JsonServiceBusMessage(object value) : ServiceBusMessage(JsonSerializer.Serialize(value))
{
}

record GreetedMessage(string Firstname, string Surname, int Age);