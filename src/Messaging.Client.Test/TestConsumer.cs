using System.Threading;
using NUnit.Framework;

namespace Messaging.Client.Test;

public class TestConsumer
{
    [Test]
    public void Should_build_consumer()
    {
        var tokenSource = new CancellationTokenSource();
        var handler = (ITestHandler) new TestHandler();
        var consumer = new MessageConsumer(null, null)
            .Register(handler, "smokey")
            .Register(handler);
        
        consumer.Consume(tokenSource.Token);
    }
}