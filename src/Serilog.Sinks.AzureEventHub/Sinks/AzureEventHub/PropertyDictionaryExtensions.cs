// SPDX-FileCopyrightText: 2017 Justin Detmar
// SPDX-License-Identifier: MIT
// Source: https://github.com/JDetmar/NLog.Extensions.AzureStorage/blob/9acec1c06306adada497671b7ef05a0a814fff30/src/NLog.Extensions.AzureEventHub/EventHubTarget.cs#L452

using System;
using System.Globalization;
using Azure.Messaging.EventHubs;
using Serilog.Events;

namespace Serilog.Sinks.AzureEventHub
{
    internal static class PropertyDictionaryExtensions
    {
        public static void AddFlattenedProperties(this EventData eventData, LogEvent logEvent)
        {
            var properties = eventData.Properties;

            foreach (var property in logEvent.Properties)
            {
                var propertyName = property.Key;
                var value = FlattenPropertyValue(propertyName, property.Value);
                if (value != null)
                {
                    properties[propertyName] = value;
                }
            }
        }

        private static object FlattenPropertyValue(string name, object value)
        {
            try
            {
                if (value is IConvertible convertible)
                {
                    return value;
                }
                else if (value is IFormattable formattable)
                {
                    return formattable.ToString(null, CultureInfo.InvariantCulture);
                }
                else
                {
                    return value?.ToString();
                }
            }
            catch (Exception ex)
            {
                Debugging.SelfLog.WriteLine("Failed converting property {0} value '{1}' to EventData: {2}", name, value?.GetType(), ex);
                return null;
            }
        }
    }
}
