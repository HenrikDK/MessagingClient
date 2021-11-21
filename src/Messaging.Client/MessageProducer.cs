using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;

namespace Messaging.Client;

public class MessageProducer
{
    private readonly IConfiguration _configuration;
    private EventHubProducerClient _producer;
    
    public MessageProducer(IConfiguration configuration)
    {
        _configuration = configuration;
        var eventHubConnectionString = _configuration.GetValue<string>("EventHubConnectionString");
        var eventHubName = _configuration.GetValue<string>("EventHubName");
        _producer = new EventHubProducerClient(eventHubConnectionString, eventHubName);
    }
    
    public void SendMessages(IList<object> messages)
    {
        try
        {
            var enveloped = messages.Select(x => new EventData(JsonSerializer.Serialize(x))
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Properties = { new KeyValuePair<string, object>("MessageName", x.GetType().Name ) },
                    ContentType = "application/json"
                }
            ).ToList();

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