using Azure.Identity;

namespace Messaging.Client;

public interface IMessageProducer
{
    /// <summary>
    /// Sends a list of messages where each message has a specific name other than the type name
    /// </summary>
    /// <param name="messages">List of name and object pairs that can be serialized as json</param>
    void SendMessages(IList<(string name, object message)> messages);
    
    /// <summary>
    /// Sends a list of messages where message name for each message is the same as the name of the type
    /// </summary>
    /// <param name="messages">List of objecst to serialize to json</param>
    void SendMessages(IList<object> messages);
}

public class MessageProducer : IMessageProducer
{
    private EventHubProducerClient _producer;
    
    /// <summary>
    /// Create a new message producer
    /// </summary>
    /// <param name="configuration">configuration object that will provide EventHubName and either EventHubFullyQualifiedNamespace or EventHubConnectionString</param>
    public MessageProducer(IConfiguration configuration)
    {
        var fullyQualifiedNamespace = configuration.GetValue<string>("EventHubFullyQualifiedNamespace");
        var eventHubName = configuration.GetValue<string>("EventHubName");

        var eventHubConnectionString = configuration.GetValue<string>("EventHubConnectionString");
        if (!string.IsNullOrEmpty(fullyQualifiedNamespace))
        {
            var credentials = new DefaultAzureCredential();
            _producer = new EventHubProducerClient(fullyQualifiedNamespace, eventHubName, credentials);
        }
        else
        {
            _producer = new EventHubProducerClient(eventHubConnectionString, eventHubName);
        }
    }
    
    public void SendMessages(IList<(string name, object message)> messages)
    {
        var enveloped = messages.Select((name, message) => new EventData(JsonSerializer.Serialize(message))
            {
                MessageId = Guid.NewGuid().ToString(),
                Properties = { new KeyValuePair<string, object>("MessageName", name ) },
                ContentType = "application/json"
            }
        ).ToList();

        SendInBatches(enveloped);
    }
    
    public void SendMessages(IList<object> messages)
    {
        var enveloped = messages.Select(x => new EventData(JsonSerializer.Serialize(x))
            {
                MessageId = Guid.NewGuid().ToString(),
                Properties = { new KeyValuePair<string, object>("MessageName", x.GetType().Name ) },
                ContentType = "application/json"
            }
        ).ToList();
        
        SendInBatches(enveloped);
    }

    private void SendInBatches(List<EventData> enveloped)
    {
        try
        {
            var batches = GetBatches(enveloped);

            foreach (var batch in batches)
            {
                _producer.SendAsync(batch);
            }
        }
        catch
        {
            // Transient failures are automatically retried
        }
    }
    
    private IList<EventDataBatch> GetBatches(IList<EventData> messages)
    {
        var result = new List<EventDataBatch>();
        var currentBatch = _producer.CreateBatchAsync().Result;

        foreach (var message in messages)
        {
            if (currentBatch.TryAdd(message)) continue;

            if (currentBatch.Count == 0)
            {
                throw new Exception("Message too large to send");
            }
            
            // finish current batch
            result.Add(currentBatch);
            
            // start new batch
            currentBatch = _producer.CreateBatchAsync().Result;
            
            // try to add message again
            if (!currentBatch.TryAdd(message))
            {
                throw new Exception("Message too large to send");
            }
        }
        
        result.Add(currentBatch);

        return result;
    }
}