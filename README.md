# Messaging client implementing a delegating consumer

Purpose of this repository is to build a messaging client that encapuslates a consistent way of using the Azure.Messaging.EventHubs.Processor and Azure.Messaging.EventHubs.Producer.

The client implements a standardized Envelope, while hiding the details of communicating, leaving the developer to focus on the business logic. 

## Settings
Both message producer and consumer support the ConnectionString or Managed Identity authentication schemes.

Settings used when using connectionString authentication:

```
{
    "EventHubConnectionString": ""
    "EventHubName": ""

    "StorageConnectionString": ""
    "StorageContainerName": ""
}
```

The first two settings are shared between consumer and producer while the last two are only used by the consumer to implement checkpointing.

By default Messaging Client will look for these values in the IConfiguration provider at setup time, but for the consumer you can use the available override to provide these settings directly.

Settings used when using Managed Identity authentication:

```
{
    "EventHubFullyQualifiedNameSpace": ""
    "EventHubName": ""

    "StorageContainerUrl": ""
}
```

As always with managed identity you avoid keeping secrets in configuration and access is entirely controlled by the identity of the machines running the instance.

## Consumer % Producer Usage

### 1. IOC Setup
Setup IMessageConsumer and IMessageProducer using your favorite DI tool. In order to use both the consumer and producer your application needs to know how to initialize it, the following example uses Lamar IOC framework.

```
using Messaging.Client;

public class MessagingClientRegistry : ServiceRegistry
{
    public MessagingClientRegistry()
    {
        For<IMessageProducer>().Use(x => new MessageProducer(x.GetInstance<IConfiguration>())).Singleton();

        For<IMessageConsumer>()
            .Use(x => new MessageConsumer(x.GetInstance<IConfiguration>(), x.GetInstance<ILogger<MessageConsumer>>()))
            .Singleton();
    }
}

```

Both IMessageProducer and IMessageConsumer are meant to be long lived singleton instances, It is not recommended to instansiate multiple consumers in the same application, as they will default based on the applications identity to using the same consumer group, and cause problems with offsets.

For more advanced scenarios it is recommended to implement a custom consumer pattern using the Azure.Messaging.EventHubs.MessageProcessor library directly.

### 2. Handlers & Messages
Implement a message handler using the generic interface IMessageHandler<T> where T is you message type, messages are deserialized using System.Text.Json, and should use either fields or properties:

```
public interface ITestHandler : IMessageHandler<TestMessage>
{
}
    
class TestHandler : ITestHandler
{
    public void Handle(TestMessage message, Guid messageId)
    {
        Console.WriteLine($"test message {message.MyDate}");
    }
}
```

Using a dedicated interface for your handler is optional but genrally recommended as it plays well with most DI's.

### 3. Setup consumer with handlers
In you applications persistent logic (such as the main running thread of a hosted service) inject the IMessageConsumer interface and any relevant handler interfaces.

The wire up the consumer with the handlers in its own running thread:

```
    var consumer = Task.Run(() =>
        {
            var tokenSource = new CancellationTokenSource();
            _consumer
                .RegisterHandler(_handleTestMessage);
    
            consumer.ConsumeWithManagedIdentity(tokenSource.Token);
        }).ContinueWith(HandleTaskCancellation, cancellationToken);

    _task.Add(consumer);
```

Note that its important to add the consumer task to the list of tasks so that they can be waited before shutdown.