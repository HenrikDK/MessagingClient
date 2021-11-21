using System;

namespace Messaging.Client.Test
{
    public interface ITestHandler : IMessageHandler<TestMessage>
    {
    }
    
    class TestHandler : ITestHandler
    {
        public void Handle(TestMessage message, Guid messageId)
        {
            Console.WriteLine($"test message {message.MyDate}");
        }
    }
}