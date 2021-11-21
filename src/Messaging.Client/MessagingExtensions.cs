namespace Messaging.Client;

public static class MessagingExtensions
{
    /// <summary>
    /// Internal method used to determine the type of the handlers parameter for deserialization purposes
    /// </summary>
    /// <param name="handler">The handler who's type has to be determined</param>
    /// <returns>Type of message the handler accepts</returns>
    public static Type GetHandlerType<T>(this IMessageHandler<T> handler) where T : class
    {
        return typeof(T);
    }
}