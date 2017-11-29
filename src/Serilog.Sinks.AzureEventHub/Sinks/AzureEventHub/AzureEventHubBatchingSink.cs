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
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.AzureEventHub
{
    /// <summary>
    /// Writes log events to an Azure Event Hub in batches.
    /// </summary>
    public class AzureEventHubBatchingSink : PeriodicBatchingSink
    {
        private readonly EventHubClient _eventHubClient;
        private readonly ITextFormatter _formatter;

        /// <summary>
        /// Construct a sink that saves log events to the specified EventHubClient.
        /// </summary>
        /// <param name="eventHubClient">The EventHubClient to use in this sink.</param>
        /// <param name="formatter">Provides formatting for outputting log data</param>
        /// <param name="batchSizeLimit"></param>
        /// <param name="period"></param>
        public AzureEventHubBatchingSink(
            EventHubClient eventHubClient,
            ITextFormatter formatter,
            int batchSizeLimit,
            TimeSpan period)
            : base(batchSizeLimit, period)
        {
            if (batchSizeLimit < 1 || batchSizeLimit > 100)
            {
                throw new ArgumentException(
                    "batchSizeLimit must be between 1 and 100.");
            }

            _eventHubClient = eventHubClient;
            _formatter = formatter;
        }

        /// <summary>
        /// Emit a batch of log events, running to completion synchronously.
        /// </summary>
        /// <param name="events">The events to emit.</param>
        protected override Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            var batchedEvents = new List<EventData>();
            var batchPartitionKey = Guid.NewGuid().ToString();

            // Possible optimizations for the below:
            // 1. Reuse a StringWriter object for the whole batch, or possibly across batches.
            // 2. Reuse byte[] buffers instead of reallocating every time.
            foreach (var logEvent in events)
            {
                byte[] body;
                using (var render = new StringWriter())
                {
                    _formatter.Format(logEvent, render);
                    body = Encoding.UTF8.GetBytes(render.ToString());
                }
                var eventHubData = new EventData(body);
                
                eventHubData.Properties.Add("Type", "SerilogEvent");

                batchedEvents.Add(eventHubData);
            }
            return _eventHubClient.SendAsync(batchedEvents, batchPartitionKey);
        }
    }
}
