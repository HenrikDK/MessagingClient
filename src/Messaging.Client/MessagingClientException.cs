namespace Messaging.Client;

public class MessagingClientException : Exception
{
    public MessagingClientException(string message) : base(message) { }
}