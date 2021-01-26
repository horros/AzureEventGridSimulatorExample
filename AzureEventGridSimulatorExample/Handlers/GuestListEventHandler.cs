using Azure.Messaging.EventGrid;
using AzureEventGridSimulatorExample.DTO;
using System;

namespace AzureEventGridSimulatorExample.Handlers
{
    public class GuestListEventHandler
    {
        internal static void HandlePersonAdded(EventGridEvent ev)
        {
            var person = ev.GetData<Person>();
            Console.WriteLine($"Got event {ev.EventType} with person {person.Firstname} {person.Lastname}");
        }
    }
}
