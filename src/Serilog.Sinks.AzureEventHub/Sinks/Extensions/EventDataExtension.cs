using Microsoft.Azure.EventHubs;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Serilog.Sinks.AzureEventHub.Extensions
{
    /// <summary>
    /// Extensions for EventData Class
    /// </summary>
    public static class EventDataExtension
    {
        /// <summary>
        /// Convert and Map LogEvent Properties to Azure Hub Event Data Properties
        /// </summary>
        /// <param name="logEvent"></param>
        /// <param name="properties"></param>
        public static void AddEventProperties(this EventData logEvent, IReadOnlyDictionary<string, LogEventPropertyValue> properties)
        {
            foreach (var item in properties)
            {
                var value = GetEventPropertyValue(item.Value);
                if (value is IDictionary<string, object> dic)
                {
                    FaltternDictionary(dic, logEvent, item.Key);
                }
                else
                {
                    logEvent.Properties.Add(item.Key, value);
                }
            }

        }

        private static void FaltternDictionary(IDictionary<string, object> nestedDictionary, EventData logEvent, string parentPropertyName)
        {
            foreach (var item in nestedDictionary)
            {
                var propertyName = $"{parentPropertyName}.{item.Key}";
                if (item.Value is IDictionary<string, object> dic)
                {
                    FaltternDictionary(dic, logEvent, propertyName);
                }
                else
                {
                    logEvent.Properties.Add(propertyName, item.Value);
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
    }
}