using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;

namespace fbognini.Sdk.Utils
{
    public enum ArrayFormatterType
    {
        None,
        UseIndex,
        JsonSerializer,
    }

    public enum ObjectFormatterType
    {
        None,
        Inline,
        JsonSerializer,
    }

    public class QueryStringBuilderFromJsonOptions
    {
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
        public ArrayFormatterType ArrayFormatterType { get; set; } = ArrayFormatterType.None;
        public ObjectFormatterType ObjectFormatterType { get; set; } = ObjectFormatterType.None;
    }

    public static class QueryStringBuilderFromJson
    {
        public static string ToQueryString<T>(this T request, string path = "", QueryStringBuilderFromJsonOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(path);

            var queryStringParameters = GetKeyValuePairs(request, options);
            if (queryStringParameters.Count == 0)
            {
                return path;
            }

            var queryString = string.Join("&", queryStringParameters.Select(kvp => $"{kvp.Key}={Uri.EscapeDataString(kvp.Value)}"));
            return path.Contains('?') ? $"{path}&{queryString}" : $"{path}?{queryString}";
        }

        public static List<KeyValuePair<string, string>> GetKeyValuePairs<T>(this T request, QueryStringBuilderFromJsonOptions? options = null)
        {
            options ??= new QueryStringBuilderFromJsonOptions();

            var inlineKeys = GetInlineKeys(typeof(T), options);

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement?>>(JsonSerializer.Serialize(request, options.JsonSerializerOptions));
            if (dict == null || dict.Count == 0)
            {
                return new List<KeyValuePair<string, string>>();
            }

            var queryStringParameters = new List<KeyValuePair<string, string>>();
            foreach (var kvp in dict.Where(x => x.Value.HasValue))
            {
                var element = kvp.Value!.Value;
                var inline = element.ValueKind == JsonValueKind.Object && (options.ObjectFormatterType == ObjectFormatterType.Inline || inlineKeys.Contains(kvp.Key));
                if (inline)
                {
                    foreach (var child in element.EnumerateObject())
                    {
                        BuildQueryString(child.Name, child.Value, queryStringParameters, options);
                    }
                }
                else
                {
                    BuildQueryString(kvp.Key, element, queryStringParameters, options);
                }
            }

            return queryStringParameters;
        }

        private static HashSet<string> GetInlineKeys(Type type, QueryStringBuilderFromJsonOptions options)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (property.GetCustomAttribute<InlineQueryStringAttribute>() is null)
                {
                    continue;
                }

                var name = property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                    ?? options.JsonSerializerOptions?.PropertyNamingPolicy?.ConvertName(property.Name)
                    ?? property.Name;

                keys.Add(name);
            }

            return keys;
        }

        private static void BuildQueryString(string key, JsonElement jsonElement, List<KeyValuePair<string, string>> queryStringParameters, QueryStringBuilderFromJsonOptions options)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Null:
                    break;
                case JsonValueKind.Object:

                    if (options.ObjectFormatterType == ObjectFormatterType.JsonSerializer)
                    {
                        var serializedObject = JsonSerializer.Serialize(jsonElement, options.JsonSerializerOptions);
                        queryStringParameters.Add(new KeyValuePair<string, string>(key, serializedObject));
                        break;
                    }

                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(key) || options.ObjectFormatterType == ObjectFormatterType.Inline
                            ? prop.Name
                            : $"{key}[{prop.Name}]";
                        BuildQueryString(newPrefix, prop.Value, queryStringParameters, options);
                    }
                    break;

                case JsonValueKind.Array:

                    if (options.ArrayFormatterType == ArrayFormatterType.JsonSerializer)
                    {
                        var serializedObject = JsonSerializer.Serialize(jsonElement, options.JsonSerializerOptions);
                        queryStringParameters.Add(new KeyValuePair<string, string>(key, serializedObject));
                        break;
                    }

                    int index = 0;
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        var newKey = options.ArrayFormatterType == ArrayFormatterType.UseIndex ? $"{key}[{index}]" : key;
                        BuildQueryString(newKey, item, queryStringParameters, options);
                        index++;
                    }
                    break;

                case JsonValueKind.String:
                    queryStringParameters.Add(new KeyValuePair<string, string>(key, jsonElement.GetString() ?? string.Empty));
                    break;
                case JsonValueKind.True:
                    queryStringParameters.Add(new KeyValuePair<string, string>(key, "true"));
                    break;
                case JsonValueKind.False:
                    queryStringParameters.Add(new KeyValuePair<string, string>(key, "false"));
                    break;
                default:
                    queryStringParameters.Add(new KeyValuePair<string, string>(key, jsonElement.GetRawText()));
                    break;
            }
        }
    }
}
