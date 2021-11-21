using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Processor;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Messaging.Client;

public interface IMessageConsumer
{
}

public class MessageConsumer : IDisposable, IMessageConsumer
{
    private readonly ILogger<MessageConsumer> _logger;
    private Dictionary<string, (Type type, Action<object, Guid> handler)> _handlers = new();
    private EventProcessorClient _processor;
    private BlobContainerClient _storageClient;
    private CancellationToken _token;
    private ConcurrentDictionary<string, int> partitionEventCount = new();


    public MessageConsumer(IConfiguration configuration, ILogger<MessageConsumer> logger)
    {
        _logger = logger;
        var consumerGroup = Assembly.GetExecutingAssembly().GetType().Namespace;
        var eventHubConnectionString = configuration.GetValue<string>("EventHubConnectionString");
        var eventHubName = configuration.GetValue<string>("EventHubName");
        var storageConnectionString = configuration.GetValue<string>("StorageConnectionString");
        var storageContainerName = configuration.GetValue<string>("StorageContainerName");
        
        _storageClient = new BlobContainerClient(storageConnectionString, storageContainerName);
        _processor = new EventProcessorClient(_storageClient, consumerGroup, eventHubConnectionString, eventHubName);

        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;
    }

    private async Task ProcessErrorHandler(ProcessErrorEventArgs arg)
    {
        try
        {
            _logger.Log(LogLevel.Error, arg.Exception, $"Event processor encountered an unhandled exception.");
        }
        catch
        {
            // Do Nothing
        }
    }

    private async Task ProcessEventHandler(ProcessEventArgs arg)
    {
        try
        {
            if (arg.CancellationToken.IsCancellationRequested)
            {
                return;
            }
        
            if (!arg.Data.Properties.ContainsKey("MessageName"))
            {
                return;
            }

            var messageName = arg.Data.Properties["MessageName"].ToString();
            if (!_handlers.ContainsKey(messageName))
            {
                return;
            }
        
            var (type, handler) = _handlers[messageName];

            var body = Encoding.UTF8.GetString(arg.Data.EventBody.ToArray());

            var message = JsonSerializer.Deserialize(body, type);
            
            try
            {
                handler.Invoke(message, new Guid(arg.Data.MessageId));
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, e, "Exception handling message, skipping");
            }
            
            var partitionId = arg.Partition.PartitionId;
            var eventsSinceLastCheckpoint = partitionEventCount.AddOrUpdate(
                key: partitionId,
                addValue: 1,
                updateValueFactory: (_, currentCount) => currentCount + 1);

            if (eventsSinceLastCheckpoint >= 50)
            {
                await arg.UpdateCheckpointAsync();
                partitionEventCount[partitionId] = 0;
            }
        }
        catch (Exception e)
        {
            // Do Nothing
        }
    }

    /// <summary>
    /// Register a handler with the consumer, uses reflection to determine message name if none is provided. 
    /// </summary>
    /// <param name="handler">An instance of IMessageHandler that will handle the specific message type.</param>
    /// <param name="messageName">Optional message name if it differs from type name</param>
    internal void RegisterHandler<T>(IMessageHandler<T> handler, string messageName = null) where T : class
    {
        var messageType = handler.GetHandlerType();

        _handlers.Add(messageName ?? messageType.Name, (messageType, handler.Handle));
    }

    /// <summary>
    /// Begin processing messages
    /// </summary>
    /// <param name="token">A cancellation token</param>
    public void Consume(CancellationToken token)
    {
        _token = token;

        _processor.StartProcessing(_token);
    }

    public void Dispose()
    {
        _processor.StopProcessing(_token);
    }
}