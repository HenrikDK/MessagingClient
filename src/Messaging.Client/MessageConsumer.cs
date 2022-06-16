namespace Messaging.Client;

public interface IMessageConsumer
{
    /// <summary>
    /// Register handler with the consumer, uses reflection to determine message name if none is provided
    /// </summary>
    /// <param name="handler">An instance of IMessageHandler that will handle the specific message type</param>
    /// <param name="messageName">Optional message name if it differs from type name</param>
    /// <returns>The consumer</returns>
    public IMessageConsumer RegisterHandler<T>(IMessageHandler<T> handler, string messageName = null) where T : class;
    
    /// <summary>
    /// Connects to the eventhub using managed identity and begin processing messages
    /// </summary>
    /// <param name="token">A cancellation token</param>
    /// <param name="eventHubId">Azure id of the eventhub from the json view (if a value is not provided "eventhub-id" is read from configuration)</param>
    /// <param name="storageContainer">Azure blob storage container url (if a value is not provided "eventhub-storage-container" is read from configuration)</param>
    public void Consume(CancellationToken token, string eventHubId = null, string storageContainer = null);
}

public class MessageConsumer : IDisposable, IMessageConsumer
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MessageConsumer> _logger;
    private Dictionary<string, (Type type, Action<object, Guid> handler)> _handlers = new();
    private EventProcessorClient _processor;
    private BlobContainerClient _storageClient;
    private CancellationToken _token;
    private ConcurrentDictionary<string, int> partitionEventCount = new();
    
    public MessageConsumer(IConfiguration configuration, ILogger<MessageConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private async Task ProcessErrorHandler(ProcessErrorEventArgs arg)
    {
        try
        {
            _logger.LogError(arg.Exception, $"Event processor encountered an unhandled exception.");
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
                _logger.LogError(e, "Exception handling message, skipping");
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
    public IMessageConsumer RegisterHandler<T>(IMessageHandler<T> handler, string messageName = null) where T : class
    {
        var messageType = handler.GetHandlerType();

        _handlers.Add(messageName ?? messageType.Name, (messageType, handler.Handle));

        return this;
    }
    
    public void Consume(CancellationToken token, string eventHubId = null, string storageContainer = null)
    {
        _token = token;
        var consumerGroup = Assembly.GetEntryAssembly().GetName().Name;
        eventHubId ??= _configuration.GetValue<string>("eventhub-id");
        storageContainer ??= _configuration.GetValue<string>("eventhub-storage-container");

        var (subscriptionId, resourceGroup, nameSpace, eventHubName) = eventHubId.ExtractValuesFromId();
        var fullyQualifiedNameSpace = $"{nameSpace}.servicebus.windows.net";
        
        var credentials = new DefaultAzureCredential();

        EnsureConsumerGroup(subscriptionId, resourceGroup, credentials, nameSpace, eventHubName, consumerGroup);

        _storageClient = new BlobContainerClient(new Uri(storageContainer), credentials);
        var options = new EventProcessorClientOptions
        {
            ConnectionOptions = new EventHubConnectionOptions
            {
                TransportType = EventHubsTransportType.AmqpWebSockets
            }
        };
        _processor = new EventProcessorClient(_storageClient, consumerGroup, fullyQualifiedNameSpace, eventHubName, credentials, options);
        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;
        
        _processor.StartProcessing(_token);

        while (_processor.IsRunning)
        {
            Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private void EnsureConsumerGroup(string subscriptionId, string resourceGroup, DefaultAzureCredential credentials, string nameSpace, string eventHubName, string consumerGroup)
    {
        var requestContext = new TokenRequestContext(new[] {"https://management.azure.com/"});
        var token = credentials.GetToken(requestContext);
        
        var serviceClientCredentials = new TokenCredentials(token.Token);
        var client = new EventHubManagementClient(serviceClientCredentials)
        {
            SubscriptionId = subscriptionId
        };
        
        client.ConsumerGroups.CreateOrUpdate(resourceGroup, nameSpace, eventHubName, consumerGroup);
    }
    
    public void Dispose()
    {
        _processor.StopProcessing(_token);
    }
}