using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal class OpenGraphNode(string id, string[] kinds)
{
    private const string NamePropertyKey = "name";
    private const string DisplayNamePropertyKey = "displayName";

    [JsonPropertyName("id")]
    public readonly string Id = id;

    /// <summary>
    /// Gets the original (platform-specific) identifier associated with the object.
    /// </summary>
    /// <remarks>
    /// This property will typically return the same value as <see cref="Id"/>,
    /// but in some instances, BloodHound may apply some transformations to the platform-specific IDs.
    /// </remarks>
    [JsonIgnore]
    public virtual string OriginalId => Id;

    [JsonPropertyName("properties")]
    public readonly SortedDictionary<string, object> Properties = [];

    [JsonPropertyName("kinds")]
    public readonly string[] Kinds = kinds;

    [JsonIgnore]
    public string? Name
    {
        get => GetProperty<string>(NamePropertyKey);
        set => SetProperty(NamePropertyKey, value);
    }

    [JsonIgnore]
    public string? DisplayName
    {
        get => GetProperty<string>(DisplayNamePropertyKey);
        set => SetProperty(DisplayNamePropertyKey, value);
    }

    public T? GetProperty<T>(string name) where T : class
    {
        if (Properties.TryGetValue(name, out var valueObj))
        {
            return valueObj as T;
        }
        else
        {
            return null;
        }
    }

    public bool? GetPropertyAsBool(string name)
    {
        if (Properties.TryGetValue(name, out var valueObj))
        {
            return valueObj as bool?;
        }
        else
        {
            return null;
        }
    }

    public void SetProperty(string name, object? value)
    {
        if (value is not null)
        {
            // Do not write empty values to the hash table / JSON
            Properties[name] = value;
        }
    }

    // Only matching by ID for now
    public OpenGraphEdgeNode ToEdgeNode() => new(Id, Kinds.Length > 0 ? Kinds[0] : null);
}
