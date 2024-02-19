﻿// Copyright 2019 Serilog Contributors
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

namespace Serilog
{
    /// <summary>
    /// Adds the `AuditTo.AzureEventHub()` extension methods to <see cref="LoggerAuditSinkConfiguration"/>.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public static class AuditLoggerConfigurationAzureEventHubExtensions
    {
        const string DefaultOutputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] {Message}{NewLine}{Exception}";

        /// <summary>
        /// A sink that puts log events into a provided Azure Event Hub.
        /// </summary>
        /// <param name="loggerConfiguration">The logger configuration.</param>
        /// <param name="eventHubClient">The Event Hub to use to insert the log entries to.</param>
        /// <param name="formatProvider">Supplies culture-specific formatting information, or null.</param>
        /// <param name="outputTemplate">A message template describing the format used to write to the sink.
        /// the default is "{Timestamp} [{Level}] {Message}{NewLine}{Exception}".</param>
        /// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            EventHubProducerClient eventHubClient,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
            )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (eventHubClient == null)
                throw new ArgumentNullException("eventHubClient");
            if (outputTemplate == null)
                throw new ArgumentNullException("outputTemplate");

            var formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);

            return AzureEventHub(loggerConfiguration, formatter, "text/plain", false, eventHubClient, restrictedToMinimumLevel);
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
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string contentType,
            bool shouldIncludeProperties,
            EventHubProducerClient eventHubClient,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (eventHubClient == null)
                throw new ArgumentNullException("eventHubClient");

            var sink = new AzureEventHubSink(eventHubClient, formatter, contentType, shouldIncludeProperties);
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
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            string connectionString,
            string eventHubName,
            string outputTemplate = DefaultOutputTemplate,
            IFormatProvider formatProvider = null,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
            )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException("eventHubName");

            var client = new EventHubProducerClient(connectionString, eventHubName);

            return AzureEventHub(loggerConfiguration, client, outputTemplate, formatProvider, restrictedToMinimumLevel);
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
        /// <returns>Logger configuration, allowing configuration to continue.</returns>
        /// <exception cref="ArgumentNullException">A required parameter is null.</exception>
        public static LoggerConfiguration AzureEventHub(
            this LoggerAuditSinkConfiguration loggerConfiguration,
            ITextFormatter formatter,
            string contentType,
            bool shouldIncludeProperties,
            string connectionString,
            string eventHubName,
            LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum
        )
        {
            if (loggerConfiguration == null)
                throw new ArgumentNullException("loggerConfiguration");
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException("connectionString");
            if (string.IsNullOrWhiteSpace(eventHubName))
                throw new ArgumentNullException("eventHubName");

            var client = new EventHubProducerClient(connectionString, eventHubName);
            return AzureEventHub(loggerConfiguration, formatter, contentType, shouldIncludeProperties, client, restrictedToMinimumLevel);
        }
    }
}
