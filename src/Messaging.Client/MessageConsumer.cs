using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

namespace Messaging.Client;

public class MessageConsumer
{
    private Dictionary<string, (Type type, Action<object, Guid> handler)> _handlers = new();

    public MessageConsumer()
    {
        // TODO
    }
    
    /// <summary>
    /// Register a handler with the consumer, uses reflection to determine message name if none is provided. 
    /// </summary>
    /// <param name="handler">An instance of IMessageHandler that will handle the specific message type.</param>
    /// <param name="messageName">Optional message name if it differs from type name</param>
    internal void RegisterHandler<T>(MessageHandler<T> handler, string messageName = null) where T : class
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
        while (!token.IsCancellationRequested)
        {
            var (type, handler) = _handlers["smokey"];

            var message = JsonSerializer.Deserialize(@"{""MyDate"":""2021-11-21T13:13:18.643852+01:00""}", type);

            handler.Invoke(message, Guid.NewGuid());
        }
    }
}