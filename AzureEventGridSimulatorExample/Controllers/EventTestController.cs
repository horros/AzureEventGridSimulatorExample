using Azure;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using AzureEventGridSimulatorExample.DTO;
using AzureEventGridSimulatorExample.Handlers;
using EventGridPublisherClientEmulator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureEventGridSimulatorExample.Controllers
{
    [Route("/EventTest")]
    public class EventTestController : Controller
    {
        private bool EventTypeNotification
                => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
                   "Notification";

        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "SubscriptionValidation";

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var jsonContent = await reader.ReadToEndAsync();

            if (EventTypeSubcriptionValidation)
            {
                return HandleValidation(jsonContent);
            } 
            else if (EventTypeNotification)
            {
                return HandleEventGridEvents(jsonContent);
            }
            return BadRequest();
        }

        private JsonResult HandleValidation(string jsonContent)
        {
            var gridEvent = EventGridEvent.Parse(jsonContent).First();
            var data = gridEvent.GetData<SubscriptionValidationEventData>();
            return new JsonResult(new
            {
                validationResponse = data.ValidationCode                
            });
        }

        private IActionResult HandleEventGridEvents(string jsonContent)
        {
            EventGridEvent[] events = EventGridEvent.Parse(jsonContent);

            foreach (EventGridEvent ev in events)
            {
                switch (ev.EventType)
                {
                    case "GuestList.PersonAdded":
                        GuestListEventHandler.HandlePersonAdded(ev);
                        break;
                    default:
                        return BadRequest();
                }
            }
            return Ok();
        }

        [Route("/SendEvent")]
        [HttpPost]
        public async Task<IActionResult> SendEvent([FromQuery]string firstname, [FromQuery]string lastname)
        {
            Uri endpoint = new Uri("https://localhost:60102/api/events?api-version=2018-01-01");
            var key = new AzureKeyCredential("DummyKey");

            ClientEmulator clientEmu = new ClientEmulator(endpoint, key);

            Person p = new Person
            {
                Firstname = firstname,
                Lastname = lastname
            };

            EventGridEvent ev = new EventGridEvent(p, Guid.NewGuid().ToString(), "GuestList.PersonAdded", "1.0");

            List<EventGridEvent> events = new List<EventGridEvent>();
            events.Add(ev);

            Response response = await clientEmu.SendEventsAsync(events);

            if (response.Status == 200)
            {
                return new OkResult();
            } 
            else
            {
                return new BadRequestResult();
            }            
        }
    }
}
