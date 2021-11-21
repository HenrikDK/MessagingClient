using System;

namespace Messaging.Client.Test
{
    class TestHandler : IMessageHandler<TestMessage>
    {
        public void Handle(Envelope<TestMessage> message)
        {
            Console.WriteLine($"test message {message.Message.MyDate}");
        }
    }
}