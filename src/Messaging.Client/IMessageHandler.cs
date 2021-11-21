namespace Messaging.Client;

public interface IMessageHandler<T>
{
    public void Handle(T message, Guid messageId);

    internal void Handle(object value, Guid messageId)
    {
        Handle((T) value, messageId);
    }
}