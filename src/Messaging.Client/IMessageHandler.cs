namespace Messaging.Client
{
    public interface IMessageHandler<T> where T: class 
    {
        public void Handle(Envelope<T> message);
    }
}