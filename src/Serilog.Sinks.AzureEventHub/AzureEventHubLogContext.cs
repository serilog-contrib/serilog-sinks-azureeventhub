using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Azure.Messaging.EventHubs;
using Serilog.Context;
using Serilog.Events;

namespace Serilog
{
    
    /// <summary>
    /// Holds ambient properties that can be attached to log events. To configure, use
    /// the Serilog.Configuration.LoggerEnrichmentConfiguration.FromLogContext method.
    /// All properties defined using AzureEventHubLogContext will be added as custom
    /// properties on Azure Event Hub message. 
    /// </summary>
    /// <remarks>
    /// The scope of the context is the current logical thread, using AsyncLocal (and
    /// so is preserved across async/await calls).
    /// </remarks>
    public static class AzureEventHubLogContext
    {
        internal static readonly string PropertyPrefix = "_AEHP_";

        /// <summary>
        /// Push a property onto the context, returning an <see cref="IDisposable"/> that must later
        /// be used to remove the property, along with any others that may have been pushed
        /// on top of it and not yet popped. The property must be popped from the same thread/logical
        /// call context.
        /// </summary>
        /// <param name="name">The name of the property.</param>
        /// <param name="value">The value of the property.</param>
        /// <returns>A handle to later remove the property from the context.</returns>
        public static IDisposable PushProperty(string name, object value) => LogContext.PushProperty($"{PropertyPrefix}{name}", value);

        internal static void PushCustomPropertiesToEventHubData(LogEvent logEvent, EventData eventHubData)
        {
            var eventHubProperties = logEvent.Properties.Where(x => x.Key.StartsWith(PropertyPrefix, StringComparison.OrdinalIgnoreCase));
            foreach (var item in eventHubProperties)
            {
                var value = GetEventPropertyValue(item.Value);
                if (value is IDictionary<string, object> dic)
                {
                    FlattenDictionary(dic, eventHubData, item.Key);
                }
                else
                {
                    eventHubData.PushPropertyWithAdjustedKey(item.Key, value);
                }
            }
        }

        private static void FlattenDictionary(IDictionary<string, object> nestedDictionary, EventData eventHubData, string parentPropertyName)
        {
            foreach (var item in nestedDictionary)
            {
                var propertyName = $"{parentPropertyName}.{item.Key}";
                if (item.Value is IDictionary<string, object> dic)
                {
                    FlattenDictionary(dic, eventHubData, propertyName);
                }
                else
                {
                    eventHubData.PushPropertyWithAdjustedKey(propertyName, item.Value);
                }
            }
        }

        private static object GetEventPropertyValue(LogEventPropertyValue data)
        {
            switch (data)
            {
                case ScalarValue value:
                    // Because it can't serialize enums
                    var isEnum = value.Value?.GetType().GetTypeInfo().IsEnum;
                    if (isEnum != null && (bool)isEnum)
                        return value.Value.ToString();
                    return value.Value;
                case DictionaryValue dictValue:
                    {
                        var expObject = new ExpandoObject() as IDictionary<string, object>;
                        foreach (var item in dictValue.Elements)
                        {
                            if (item.Key.Value is string key)
                                expObject.Add(key, GetEventPropertyValue(item.Value));
                        }
                        return expObject;
                    }

                case SequenceValue seq:
                    var sequenceItems = seq.Elements.Select(GetEventPropertyValue).ToArray();
                    return string.Join(", ", sequenceItems);

                case StructureValue str:
                    try
                    {
                        if (str.TypeTag == null)
                            return str.Properties.ToDictionary(p => p.Name, p => GetEventPropertyValue(p.Value));

                        if (!str.TypeTag.StartsWith("DictionaryEntry") && !str.TypeTag.StartsWith("KeyValuePair"))
                            return str.Properties.ToDictionary(p => p.Name, p => GetEventPropertyValue(p.Value));

                        var key = GetEventPropertyValue(str.Properties[0].Value);
                        if (key == null)
                            return null;

                        var expObject = new ExpandoObject() as IDictionary<string, object>;
                        expObject.Add(key.ToString(), GetEventPropertyValue(str.Properties[1].Value));
                        return expObject;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    break;
            }

            return null;
        }

        private static void PushPropertyWithAdjustedKey(this EventData eventData, string key, object value)
        {
            eventData.Properties.Add(key.Replace(PropertyPrefix, string.Empty), value);
        }
    }
}
