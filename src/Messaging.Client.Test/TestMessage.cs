using System;

namespace Messaging.Client.Test;

class TestMessage : IMessage
{
    public DateTime MyDate { get; set; }
}