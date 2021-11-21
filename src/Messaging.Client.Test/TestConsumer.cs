using System.Threading;
using NUnit.Framework;

namespace Messaging.Client.Test;

public class TestConsumer
{
    [Test]
    public void Should_build_consumer()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = new TestHandler();
        var consumer = new MessageConsumer()
            .Register(handler, "smokey")
            .Register(handler, "lemon");
        
        consumer.Consume(tokenSource.Token);
    }
}