using System.Text.Json;
using System.Text.Json.Serialization;
using fbognini.Sdk.Utils;
using Xunit;

namespace fbognini.Sdk.Tests;

public class QueryStringBuilderFromJsonTests
{
    private sealed class Paging
    {
        [JsonPropertyName("length")] public int? PageSize { get; set; }
        [JsonPropertyName("start")] public string? Start { get; set; }
        [JsonPropertyName("sort-by")] public List<string>? SortBy { get; set; }
    }

    private sealed class ScalarRequest
    {
        public string? Name { get; set; }
        public int Age { get; set; }
    }

    private sealed class PlainNestedRequest
    {
        public string? Q { get; set; }
        public Paging Paging { get; set; } = new();
    }

    private sealed class InlineNestedRequest
    {
        public string? Q { get; set; }
        [InlineQueryString] public Paging Paging { get; set; } = new();
    }

    private sealed class ArrayRequest
    {
        public List<string> Tags { get; set; } = new();
    }

    [Fact]
    public void Scalars_are_rendered_as_flat_keys()
    {
        var result = new ScalarRequest { Name = "bob", Age = 3 }.ToQueryString("path");

        Assert.Equal("path?Name=bob&Age=3", result);
    }

    [Fact]
    public void Null_values_are_skipped()
    {
        var result = new ScalarRequest { Name = null, Age = 3 }.ToQueryString();

        Assert.Equal("?Age=3", result);
    }

    [Fact]
    public void Nested_object_without_attribute_uses_bracket_notation()
    {
        var result = new PlainNestedRequest { Q = "hello", Paging = new Paging { PageSize = 10, Start = "20" } }
            .ToQueryString();

        Assert.Equal("?Q=hello&Paging[length]=10&Paging[start]=20", result);
    }

    [Fact]
    public void Inline_attribute_flattens_only_the_marked_property()
    {
        var result = new InlineNestedRequest { Q = "hello", Paging = new Paging { PageSize = 10, Start = "20" } }
            .ToQueryString();

        Assert.Equal("?Q=hello&length=10&start=20", result);
    }

    [Fact]
    public void Inline_attribute_respects_child_JsonPropertyName_on_arrays()
    {
        var result = new InlineNestedRequest
        {
            Paging = new Paging { PageSize = 10, SortBy = new List<string> { "name", "date" } },
        }.ToQueryString();

        Assert.Equal("?length=10&sort-by=name&sort-by=date", result);
    }

    [Fact]
    public void InlineNestedObjects_option_flattens_every_nested_object()
    {
        var options = new QueryStringBuilderFromJsonOptions { ObjectFormatterType = ObjectFormatterType.Inline };

        var result = new PlainNestedRequest { Q = "hello", Paging = new Paging { PageSize = 10, Start = "20" } }
            .ToQueryString("", options);

        Assert.Equal("?Q=hello&length=10&start=20", result);
    }

    [Fact]
    public void Arrays_repeat_the_key_by_default()
    {
        var result = new ArrayRequest { Tags = new List<string> { "a", "b" } }.ToQueryString();

        Assert.Equal("?Tags=a&Tags=b", result);
    }

    [Fact]
    public void Arrays_use_index_when_option_is_set()
    {
        var options = new QueryStringBuilderFromJsonOptions { ArrayFormatterType = ArrayFormatterType.UseIndex };

        var result = new ArrayRequest { Tags = new List<string> { "a", "b" } }.ToQueryString("", options);

        Assert.Equal("?Tags[0]=a&Tags[1]=b", result);
    }

    [Fact]
    public void Camel_case_naming_policy_is_applied_to_keys()
    {
        var options = new QueryStringBuilderFromJsonOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
        };

        var result = new ScalarRequest { Name = "bob", Age = 3 }.ToQueryString("", options);

        Assert.Equal("?name=bob&age=3", result);
    }

    [Fact]
    public void Inline_attribute_resolves_key_under_camel_case_policy()
    {
        var options = new QueryStringBuilderFromJsonOptions
        {
            JsonSerializerOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase },
        };

        var result = new InlineNestedRequest { Q = "hello", Paging = new Paging { PageSize = 10, Start = "20" } }
            .ToQueryString("", options);

        Assert.Equal("?q=hello&length=10&start=20", result);
    }
}
