using System;

namespace Messaging.Client.Test
{
    class TestHandler : MessageHandler<TestMessage>
    {
        public override void Handle(TestMessage message, Guid messageId)
        {
            Console.WriteLine($"test message {message.MyDate}");
        }
    }
}