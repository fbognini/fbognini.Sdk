using System;

namespace fbognini.Sdk.Utils
{
    /// <summary>
    /// Marks a property whose (object) value must be inlined into the parent query string: its members are emitted as top-level keys instead of <c>property[child]</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class InlineQueryStringAttribute : Attribute
    {
    }
}
