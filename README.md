# Messaging client implementing a delegating consumer

Purpose of this repository is to build a messaging client that encapuslates a consistent way of using the Azure.Messaging.EventHubs.Processor and Azure.Messaging.EventHubs.Producer.

The client implements a standardized Envelope, while hiding the details of communicating, leaving the developer to focus on the business logic. 
