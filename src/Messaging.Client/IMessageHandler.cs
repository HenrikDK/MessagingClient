namespace Messaging.Client
{
    public interface IMessageHandler<T> where T: class, IMessage 
    {
        public void Handle(MessageEnvelope<T> message);
    }
}