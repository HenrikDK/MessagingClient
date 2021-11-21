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
    
    public MessageProducer(IConfiguration configuration, string topic)
    {
        _configuration = configuration;
        _producer = new EventHubProducerClient(_configuration.GetValue<string>("event-hub"), topic);
    }
    
    public void SendMessages(IList<object> messages)
    {
        // TODO handle batching.
        var enveloped = messages.Select(x => new EventData(JsonSerializer.Serialize(x))
            {
                MessageId = Guid.NewGuid().ToString(),
                Properties = { new KeyValuePair<string, object>("MessageName", x.GetType().Name ) },
                ContentType = "json"
            }
        );

        _producer.SendAsync(enveloped).Wait();
    }
}