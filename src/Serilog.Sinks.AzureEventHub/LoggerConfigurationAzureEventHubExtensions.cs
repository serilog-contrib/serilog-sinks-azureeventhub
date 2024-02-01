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
using Azure.Messaging.EventHubs.Producer;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using Serilog.Sinks.AzureEventHub;
using Serilog.Sinks.PeriodicBatching;

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
            EventHubProducerClient eventHubClient,
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

            return AzureEventHub(loggerConfiguration, formatter, "text/plain", false, eventHubClient, restrictedToMinimumLevel, writeInBatches, period, batchPostingLimit);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="contentType">Content type that the <paramref name="formatter"/> produces.</param>
        /// <param name="shouldIncludeProperties">Should the properties be included in the event data. You probably do not want this when using a JSON formatted that includes properties already.</param>
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
            string contentType,
            bool shouldIncludeProperties,
            EventHubProducerClient eventHubClient,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
            bool writeInBatches = false,
            TimeSpan? period = null,
            int? batchPostingLimit = null)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (eventHubClient == null)
                throw new ArgumentNullException("eventHubClient");

            var batchSizeLimit = batchPostingLimit ?? DefaultBatchPostingLimit;
            if (batchSizeLimit < 1 || batchSizeLimit > 100)
            {
                throw new ArgumentException(
                    "batchSizeLimit must be between 1 and 100.", nameof(batchPostingLimit));
            }

            ILogEventSink sink;
            if (writeInBatches)
            {
                var eventHubSink = new AzureEventHubBatchingSink(
                    eventHubClient,
                    formatter,
                    contentType,
                    shouldIncludeProperties);
                var batchingOptions = new PeriodicBatchingSinkOptions
                {
                    BatchSizeLimit = batchSizeLimit,
                    Period = period ?? DefaultPeriod,
                    EagerlyEmitFirstEvent = true,
                    QueueLimit = 10000
                };

                sink = new PeriodicBatchingSink(eventHubSink, batchingOptions);
            }
            else
            {
                sink = new AzureEventHubSink(eventHubClient, formatter, contentType, shouldIncludeProperties);
            }

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

            var client = new EventHubProducerClient(connectionString, eventHubName);

            return AzureEventHub(loggerConfiguration, client, outputTemplate, formatProvider, restrictedToMinimumLevel, writeInBatches, period, batchPostingLimit);
        }

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="formatter">Formatter used to convert log events to text.</param>
        /// <param name="contentType">Content type that the <paramref name="formatter"/> produces.</param>
        /// <param name="shouldIncludeProperties">Should the properties be included in the event data. You probably do not want this when using a JSON formatted that includes properties already.</param>
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
            string contentType,
            bool shouldIncludeProperties,
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

            var client = new EventHubProducerClient(connectionString, eventHubName);

            return AzureEventHub(loggerConfiguration, formatter, contentType, shouldIncludeProperties, client, restrictedToMinimumLevel, writeInBatches, period, batchPostingLimit);
        }
    }
}
