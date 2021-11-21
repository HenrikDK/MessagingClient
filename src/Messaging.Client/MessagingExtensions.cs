using System;

namespace Messaging.Client;

public static class MessagingExtensions
{
    /// <summary>
    /// Internal method used to determine the type of the handlers parameter for deserialization purposes
    /// </summary>
    /// <param name="handler">The handler who's type has to be determined</param>
    /// <returns>Type of message the handler accepts</returns>
    public static Type GetHandlerType<T>(this MessageHandler<T> handler) where T : class
    {
        return typeof(T);
    }
    
    /// <summary>
    /// register a handler with the consumer, uses reflection to determine message name if none is provided. 
    /// </summary>
    /// <param name="consumer">The event consumer that will process the messages</param>
    /// <param name="handler">An instance of IMessageHandler that will handle the specific message type.</param>
    /// <param name="messageName">Optional message name if it differs from type name</param>
    /// <returns></returns>
    public static MessageConsumer Register<T>(this MessageConsumer consumer, MessageHandler<T> handler, string messageName = null) where T : class
    {
        consumer.RegisterHandler(handler, messageName);

        return consumer;
    }
}