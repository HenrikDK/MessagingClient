using System;

namespace Messaging.Client.Test
{
    class TestHandler : IMessageHandler<TestMessage>
    {
        public void Handle(MessageEnvelope<TestMessage> message)
        {
            throw new NotImplementedException();
        }
    }
}