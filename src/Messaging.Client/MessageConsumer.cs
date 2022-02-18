using Azure.Identity;

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
    /// Begin processing
    /// </summary>
    /// <param name="token">A cancellation token</param>
    /// <param name="eventHubConnectionString">Nullable will default to read EventHubConnectionString from configuration if not provided</param>
    /// <param name="eventHubName">Nullable will default to read EventHubName from configuration if not provided</param>
    /// <param name="storageConnectionString">Nullable will default to read StorageConnectionString from configuration if not provided</param>
    /// <param name="storageContainerName">Nullable will default to read StorageContainerName from configuration if not provided</param>
    public void Consume(CancellationToken token, string eventHubConnectionString = null, string eventHubName = null, string storageConnectionString = null, string storageContainerName = null);
    
    /// <summary>
    /// Begin processing
    /// </summary>
    /// <param name="token">A cancellation token</param>
    /// <param name="fullyQualifiedNameSpace">Nullable will default to read EventHubFullyQualifiedNameSpace from configuration if not provided</param>
    /// <param name="eventHubName">Nullable will default to read EventHubName from configuration if not provided</param>
    /// <param name="storageContainerUrl">Nullable will default to read StorageContainerUrl from configuration if not provided</param>
    public void ConsumeWithManagedIdentity(CancellationToken token, string fullyQualifiedNameSpace = null, string eventHubName = null, string storageContainerUrl = null);
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

    public void Consume(CancellationToken token, string eventHubConnectionString = null, string eventHubName = null, string storageConnectionString = null, string storageContainerName = null)
    {
        _token = token;
        var consumerGroup = Assembly.GetEntryAssembly().GetName().Name;
        eventHubConnectionString ??= _configuration.GetValue<string>("EventHubConnectionString");
        eventHubName ??= _configuration.GetValue<string>("EventHubName");
        storageConnectionString ??= _configuration.GetValue<string>("StorageConnectionString");
        storageContainerName ??= _configuration.GetValue<string>("StorageContainerName");
        
        _storageClient = new BlobContainerClient(storageConnectionString, storageContainerName);
        _processor = new EventProcessorClient(_storageClient, consumerGroup, eventHubConnectionString, eventHubName);
        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;
        
        _processor.StartProcessing(_token);

        while (_processor.IsRunning)
        {
            Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    public void ConsumeWithManagedIdentity(CancellationToken token, string fullyQualifiedNameSpace = null, string eventHubName = null, string storageContainerUrl = null)
    {
        _token = token;
        var consumerGroup = Assembly.GetEntryAssembly().GetName().Name;
        fullyQualifiedNameSpace ??= _configuration.GetValue<string>("EventHubFullyQualifiedNameSpace");
        eventHubName ??= _configuration.GetValue<string>("EventHubName");
        storageContainerUrl ??= _configuration.GetValue<string>("StorageContainerUrl");

        var credentials = new DefaultAzureCredential();
            
        _storageClient = new BlobContainerClient(new Uri(storageContainerUrl), credentials);
        _processor = new EventProcessorClient(_storageClient, consumerGroup, fullyQualifiedNameSpace, eventHubName, credentials);
        _processor.ProcessEventAsync += ProcessEventHandler;
        _processor.ProcessErrorAsync += ProcessErrorHandler;
        
        _processor.StartProcessing(_token);

        while (_processor.IsRunning)
        {
            Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
    
    public void Dispose()
    {
        _processor.StopProcessing(_token);
    }
}