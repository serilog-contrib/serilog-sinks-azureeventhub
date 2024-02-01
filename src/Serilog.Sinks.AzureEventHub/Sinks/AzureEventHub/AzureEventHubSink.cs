// Copyright 2014 Serilog Contributors
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Producer;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub.
    /// </summary>
    public class AzureEventHubSink : ILogEventSink
    {
        private readonly EventHubProducerClient _eventHubClient;
        private readonly ITextFormatter _formatter;
        private readonly string _contentType;
        private readonly bool _shouldIncludeProperties;

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        /// <param name="contentType">Content type that the <paramref name="formatter"/> produces.</param>
        /// <param name="shouldIncludeProperties">Should the properties be included in the event data.</param>
        public AzureEventHubSink(
            EventHubProducerClient eventHubClient,
            ITextFormatter formatter,
            string contentType,
            bool shouldIncludeProperties)
        {
            _eventHubClient = eventHubClient;
            _formatter = formatter;
            _contentType = contentType;
            _shouldIncludeProperties = shouldIncludeProperties;
        }

        /// <summary>
        /// Emit the provided log event to the sink.
        /// </summary>
        /// <param name="logEvent">The log event to write.</param>
        public void Emit(LogEvent logEvent)
        {
            byte[] body;
            using (var render = new StringWriter())
            {
                _formatter.Format(logEvent, render);
                body = Encoding.UTF8.GetBytes(render.ToString());
            }

            var eventHubData = EventHubsModelFactory.EventData(new BinaryData(body));
            if (!string.IsNullOrWhiteSpace(_contentType))
            {
                eventHubData.ContentType = _contentType;
            }

            eventHubData.Properties.Add("Timestamp", logEvent.Timestamp);
            eventHubData.Properties.Add("Type", "SerilogEvent");
            eventHubData.Properties.Add("Level", logEvent.Level.ToString());

            if (logEvent.TraceId != null)
            {
                eventHubData.Properties.Add(nameof(logEvent.TraceId), logEvent.TraceId?.ToString());
            }

            if (logEvent.SpanId != null)
            {
                eventHubData.Properties.Add(nameof(logEvent.SpanId), logEvent.SpanId?.ToString());
            }

            if (logEvent.Exception != null)
            {
                eventHubData.Properties.Add(nameof(logEvent.Exception), logEvent.Exception);
            }

            if (_shouldIncludeProperties)
            {
                eventHubData.AddFlattenedProperties(logEvent);
            }

            //Unfortunately no support for async in Serilog yet
            //https://github.com/serilog/serilog/issues/134
            _eventHubClient.SendAsync(new List<EventData>() { eventHubData } , new SendEventOptions() { PartitionKey = Guid.NewGuid().ToString() }).GetAwaiter().GetResult();
        }
    }
}
