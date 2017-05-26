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
using System.Diagnostics.CodeAnalysis;
using Microsoft.ServiceBus.Messaging;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.AzureEventHub;

namespace Serilog
{
    /// <summary>
    /// Adds the WriteTo.AzureEventHub() extension metho to <see cref="LoggerConfiguration"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class LoggerConfigurationAzureEventHubExtensions
    {
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        /// <summary>
        /// A reasonable default for the number of events posted in each batch.
        /// </summary>
        private const int DefaultBatchPostingLimit = 50;

        /// <summary>
        /// A reasonable default time to wait between checking for event batches.
        /// </summary>
        private static readonly TimeSpan DefaultPeriod = TimeSpan.FromSeconds(2);

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            EventHubClient eventHubClient,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null
            )
        {
            if (loggerConfiguration == null) 
                throw new ArgumentNullException("loggerConfiguration");
            if (eventHubClient == null)
                throw new ArgumentNullException("eventHubClient");
            if (outputTemplate == null) 
                throw new ArgumentNullException("outputTemplate");

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return AzureEventHub(loggerConfiguration, formatter, eventHubClient, restrictedToMinimumLevel,writeInBatches, period, batchPostingLimit);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            EventHubClient eventHubClient,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (eventHubClient == null)
                throw new ArgumentNullException("eventHubClient");

            var sink = writeInBatches ?
                (ILogEventSink)new AzureEventHubBatchingSink(
                    eventHubClient,
                    formatter,
                    batchPostingLimit ?? DefaultBatchPostingLimit,
                    period ?? DefaultPeriod) :
                new AzureEventHubSink(eventHubClient, formatter);

            return loggerConfiguration.Sink(sink, restrictedToMinimumLevel);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="connectionString">The Event Hub connection string.</param>
        /// <param name="eventHubName">The Event Hub name.</param>
        /// /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            string connectionString,
            string eventHubName,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null
            )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException("eventHubName");

            var client = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);

            return AzureEventHub(loggerConfiguration, client, outputTemplate, formatProvider, restrictedToMinimumLevel, writeInBatches, period, batchPostingLimit);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="connectionString">The Event Hub connection string.</param>
        /// <param name="eventHubName">The Event Hub name.</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <param name="writeInBatches">Use a periodic batching sink, as opposed to a synchronous one-at-a-time sink; this alters the partition
        /// key used for the events so is not enabled by default.</param>
        /// <param name="batchPostingLimit">The maximum number of events to post in a single batch.</param>
        /// <param name="period">The time to wait between checking for event batches.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string connectionString,
            string eventHubName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null
        )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException("eventHubName");

            var client = EventHubClient.CreateFromConnectionString(connectionString, eventHubName);

            return AzureEventHub(loggerConfiguration, formatter, client, restrictedToMinimumLevel, writeInBatches, period, batchPostingLimit);
        }
    }
}
