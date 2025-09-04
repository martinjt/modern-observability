# Modern Observability in .NET Workshop

This project supports the Modern Observability in .NET workshop.

It's designed to be simplistic and provide a base solution to show some advanced instrumentation techniques that will help people understand how to effectively enhance the telemetry and use it better.

## Overview

There are three main components to the solution:

1. Greeter - The single entry point to the system initiating the flow.
1. Age Generator - Shows HTTP Client instrumentation and propagation
1. Email Sender - Shows how to propagate over a Messaging workflow.

There is an AppHost project that uses Aspire to orchestrate the solution which lives in `infra/` and some `.http` test files in the `http/` folder.

## Running the solution

```shell
dotnet run --project infra/ModernObservability.AppHost
```

## Testing the solution

There are some `.http` test files in the `http/` folder.  You can use these to test the solution.  You can use the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client) for Visual Studio Code to run these tests.

## External components

In addition to the projects, this solution will also run the following external components:

1. Papercut SMTP - A local SMTP server for testing email.  This is used to send the emails from the Email Sender.
2. Service Bus Emulator - A local Service Bus emulator for testing messaging.  This is used to send the messages between the Greeter and Email Sender.
