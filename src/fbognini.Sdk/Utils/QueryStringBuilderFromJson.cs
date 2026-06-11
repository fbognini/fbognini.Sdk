using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace fbognini.Sdk.Utils
{
    public class QueryStringBuilderFromJsonOptions
    {
        public bool UseIndexForArrays { get; set; } = false;

        /// <summary>When true, every nested object is inlined (its members become top-level keys).</summary>
        public bool InlineNestedObjects { get; set; } = false;

        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    }

    public static class QueryStringBuilderFromJson
    {
        public static string ToQueryString<T>(this T request, string path = "", QueryStringBuilderFromJsonOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(path);

            options ??= new QueryStringBuilderFromJsonOptions();

            var inlineKeys = GetInlineKeys(typeof(T), options);

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement?>>(JsonSerializer.Serialize(request, options.JsonSerializerOptions));
            if (dict == null || dict.Count == 0)
            {
                return path;
            }

            var queryParams = new List<string>();
            foreach (var kvp in dict.Where(x => x.Value.HasValue))
            {
                var element = kvp.Value!.Value;
                var inline = element.ValueKind == JsonValueKind.Object
                    && (options.InlineNestedObjects || inlineKeys.Contains(kvp.Key));

                if (inline)
                {
                    foreach (var child in element.EnumerateObject())
                    {
                        BuildQueryString(child.Name, child.Value, queryParams, options);
                    }
                }
                else
                {
                    BuildQueryString(kvp.Key, element, queryParams, options);
                }
            }

            if (queryParams.Count == 0)
            {
                return path;
            }

            var queryString = string.Join("&", queryParams);
            return path.Contains('?') ? $"{path}&{queryString}" : $"{path}?{queryString}";
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

        private static void BuildQueryString(string key, JsonElement jsonElement, List<string> queryParams, QueryStringBuilderFromJsonOptions options)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Null:
                    break;
                case JsonValueKind.Object:
                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(key) || options.InlineNestedObjects
                            ? prop.Name
                            : $"{key}[{prop.Name}]";
                        BuildQueryString(newPrefix, prop.Value, queryParams, options);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in jsonElement.EnumerateArray())
                    {
                        var newKey = options.UseIndexForArrays ? $"{key}[{index}]" : key;
                        BuildQueryString(newKey, item, queryParams, options);
                        index++;
                    }
                    break;

                default:
                    queryParams.Add($"{key}={Uri.EscapeDataString(jsonElement.ToString())}");
                    break;
            }
        }
    }
}
