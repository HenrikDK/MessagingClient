using System;

namespace Messaging.Client
{
    public abstract class MessageHandler<T> where T : class
    {
        public void Handle(object value, Guid messageId)
        {
            Handle((T) value, messageId);
        }

        public abstract void Handle(T message, Guid messageId);
    }
}