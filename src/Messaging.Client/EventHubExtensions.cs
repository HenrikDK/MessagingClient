namespace Messaging.Client;

public static class EventHubExtensions
{
    /// <summary>
    /// Internal method used to extract information about the eventhub for further use by the eventHub client
    /// </summary>
    /// <param name="eventHubId">EventHub id from the portal pages json view</param>
    /// <returns>tuple containing subscriptionId, resourceGroup, namespace, eventhub name</returns>
    /// <exception cref="Exception"></exception>
    public static (string subscriptionId, string resourceGroup, string nameSpace, string eventHubName) ExtractValuesFromId(this string eventHubId)
    {
        var subscriptionKey = "subscriptions/";
        if (eventHubId.IndexOf(subscriptionKey) < 0)
        {
            throw new MessagingClientException("Invalid eventhub id, missing subscription id");
        }

        var subscriptionId = eventHubId.Substring(eventHubId.IndexOf(subscriptionKey) + subscriptionKey.Length);
        subscriptionId = subscriptionId.Substring(0, subscriptionId.IndexOf("/"));

        var resourceGroupKey = "resourceGroups/";
        if (eventHubId.IndexOf(resourceGroupKey) < 0)
        {
            throw new MessagingClientException("Invalid eventhub id, missing resource group");
        }

        var resourceGroup = eventHubId.Substring(eventHubId.IndexOf(resourceGroupKey) + resourceGroupKey.Length);
        resourceGroup = resourceGroup.Substring(0, resourceGroup.IndexOf("/"));

        var namespaceKey = "namespaces/";
        if (eventHubId.IndexOf(namespaceKey) < 0)
        {
            throw new MessagingClientException("Invalid eventhub id, missing namespace");
        }

        var nameSpace = eventHubId.Substring(eventHubId.IndexOf(namespaceKey) + namespaceKey.Length);
        nameSpace = nameSpace.Substring(0, nameSpace.IndexOf("/"));

        var nameKey = "eventhubs/";
        if (eventHubId.IndexOf(nameKey) < 0)
        {
            throw new MessagingClientException("Invalid eventhub id, missing eventhub name");
        }

        var eventhubName = eventHubId.Substring(eventHubId.IndexOf(nameKey) + nameKey.Length);

        return (subscriptionId, resourceGroup, nameSpace, eventhubName);
    }
}