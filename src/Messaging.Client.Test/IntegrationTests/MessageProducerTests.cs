using System;
using System.Collections.Generic;
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

public class MessageProducerTests
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
        registry.For<IMessageProducer>()
            .Use(x => new MessageProducer(x.GetInstance<IConfiguration>(), x.GetInstance<ILogger<MessageProducer>>()))
            .Singleton();
        _container = new Container(registry);
    }
    
    //[Test]
    public void Should_post_messages_to_azure_event_hub()
    {
        var producer = _container.GetInstance<IMessageProducer>();
        
        var message = new TestMessage
        {
            MyDate = DateTime.Now
        };
        
        producer.SendMessages(new List<object>{message});
    }
}