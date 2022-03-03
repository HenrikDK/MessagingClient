using System.Threading;
using FluentAssertions;
using Messaging.Client.Test.CodeUsageTests;
using NUnit.Framework;

namespace Messaging.Client.Test.UnitTests;

public class EventHubExtensionTests
{
    [Test]
    public void Should_parse_eventhub_id_correctly()
    {
        // eventhubId from the portals json view
        var eventHubId = "/subscriptions/<subscription-id>/resourceGroups/<resourceGroup>/providers/Microsoft.EventHub/namespaces/<namespace>/eventhubs/<eventhub-name>";
        
        var (subscriptionId, resourceGroup, nameSpace, eventHubName) = eventHubId.ExtractValuesFromId();

        subscriptionId.Should().Be("<subscription-id>");
        resourceGroup.Should().Be("<resourceGroup>");
        nameSpace.Should().Be("<namespace>");
        eventHubName.Should().Be("<eventhub-name>");
    }

    [Test]
    public void Should_throw_exception_on_missing_subscription_element()
    {
        // eventhubId from the portals json view
        var eventHubId = "/resourceGroups/<resourceGroup>/providers/Microsoft.EventHub/namespaces/<namespace>/eventhubs/<eventhub-name>";

        eventHubId.Invoking(y => y.ExtractValuesFromId())
            .Should().Throw<MessagingClientException>()
            .WithMessage("Invalid eventhub id, missing subscription id");
    }
    
    [Test]
    public void Should_throw_exception_on_missing_resource_group_element()
    {
        // eventhubId from the portals json view
        var eventHubId = "/subscriptions/<subscription-id>/providers/Microsoft.EventHub/namespaces/<namespace>/eventhubs/<eventhub-name>";

        eventHubId.Invoking(y => y.ExtractValuesFromId())
            .Should().Throw<MessagingClientException>()
            .WithMessage("Invalid eventhub id, missing resource group");
    }
    [Test]
    public void Should_throw_exception_on_missing_namespace_element()
    {
        // eventhubId from the portals json view
        var eventHubId = "/subscriptions/<subscription-id>/resourceGroups/<resourceGroup>/providers/Microsoft.EventHub/eventhubs/<eventhub-name>";

        eventHubId.Invoking(y => y.ExtractValuesFromId())
            .Should().Throw<MessagingClientException>()
            .WithMessage("Invalid eventhub id, missing namespace");
    }
    [Test]
    public void Should_throw_exception_on_missing_name_element()
    {
        // eventhubId from the portals jsonview
        var eventHubId = "/subscriptions/<subscription-id>/resourceGroups/<resourceGroup>/providers/Microsoft.EventHub/namespaces/<namespace>/";

        eventHubId.Invoking(y => y.ExtractValuesFromId())
            .Should().Throw<MessagingClientException>()
            .WithMessage("Invalid eventhub id, missing eventhub name");
    }
}