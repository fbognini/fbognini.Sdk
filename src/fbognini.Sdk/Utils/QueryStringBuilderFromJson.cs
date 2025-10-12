using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace fbognini.Sdk.Utils
{
    public class QueryStringBuilderFromJsonOptions
    {
        public bool UseIndexForArrays { get; set; } = false;
        public JsonSerializerOptions? JsonSerializerOptions { get; set; }
    }

    public static class QueryStringBuilderFromJson
    {
        public static string ToQueryString<T>(this T request, string path = "", QueryStringBuilderFromJsonOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(path);

            options ??= new QueryStringBuilderFromJsonOptions();

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(request, options.JsonSerializerOptions));
            if (dict == null || dict.Count == 0)
            {
                return path;
            }

            var queryParams = new List<string>();
            foreach (var kvp in dict)
            {
                BuildQueryString(kvp.Key, kvp.Value, queryParams, options);
            }

            var queryString = string.Join("&", queryParams);
            return path.Contains('?') ? $"{path}&{queryString}" : $"{path}?{queryString}";
        }

        private static void BuildQueryString(string key, JsonElement jsonElement, List<string> queryParams, QueryStringBuilderFromJsonOptions options)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var prop in jsonElement.EnumerateObject())
                    {
                        string newPrefix = string.IsNullOrEmpty(key)
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
