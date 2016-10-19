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
using Microsoft.ServiceBus.Messaging;
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
        readonly EventHubClient _eventHubClient;
        readonly ITextFormatter _formatter;
        readonly IDictionary<string, string> _customProperties; 

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        public AzureEventHubSink(
            EventHubClient eventHubClient,
            ITextFormatter formatter,
            IDictionary<string,string> customProperties)
        {
            _eventHubClient = eventHubClient;
            _formatter = formatter;
            _customProperties = customProperties;
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
            var eventHubData = new EventData(body)
            {
                PartitionKey = Guid.NewGuid().ToString()
            };

            if (_customProperties != null)
            {
                foreach (var prop in _customProperties)
                {
                    eventHubData.Properties.Add(prop.Key, prop.Value);
                }
            }

            _eventHubClient.Send(eventHubData);
        }
    }
}