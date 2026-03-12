using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

public sealed record PropertyMatcher
{
    private const string EqualsOperator = "equals";

    [JsonPropertyName("key")]
    public string Property { get; init; }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Kept as instance member for model serialization consistency.")]
    [JsonPropertyName("operator")]
    public string Operator => EqualsOperator;

    [JsonPropertyName("value")]
    public string Value { get; init; }

    public PropertyMatcher(string property, string value)
    {
        ArgumentNullException.ThrowIfNull(property);
        ArgumentNullException.ThrowIfNull(value);

        this.Property = property;
        this.Value = value;
    }
}
