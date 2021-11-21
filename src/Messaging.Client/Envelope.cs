using System;

namespace Messaging.Client
{
    public class MessageEnvelope<T> where T : class
    {
        public Guid MessageId { get; set; }
        public DateTime SentAt { get; set; }
        public T Message { get; set; }
    }
}