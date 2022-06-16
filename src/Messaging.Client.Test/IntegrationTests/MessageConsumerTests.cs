using System;
using System.IO;
using System.Threading;
using Lamar;
using Messaging.Client.Test.CodeUsageTests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NUnit.Framework;

namespace Messaging.Client.Test.IntegrationTests;

public class MessageConsumerTests
{
    private IConfiguration _configuration;
    private Container _container;
    
    [SetUp]
    public void Setup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        _configuration = builder.Build();

        var registry = new ServiceRegistry();
        registry.AddSingleton(_configuration);
        registry.For<IMessageConsumer>()
            .Use(x => new MessageConsumer(x.GetInstance<IConfiguration>(), Substitute.For<ILogger<MessageConsumer>>()))
            .Singleton();
        _container = new Container(registry);
    }
    
    //[Test]
    public void Should_listen_to_messages_on_azure_event_hub()
    {
        var consumer = _container.GetInstance<IMessageConsumer>();

        var tokenSource = new CancellationTokenSource();
        tokenSource.CancelAfter(TimeSpan.FromMinutes(2));
        var handler = (ITestHandler) new TestHandler();
        consumer.RegisterHandler(handler);
        
        consumer.Consume(tokenSource.Token);
    }
}