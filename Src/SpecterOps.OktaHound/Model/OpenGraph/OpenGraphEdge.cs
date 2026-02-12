using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal sealed class OpenGraphEdge
{

    [JsonPropertyName("kind")]
    public readonly string Kind;

    [JsonPropertyName("start")]
    public readonly OpenGraphEdgeNode Start;

    [JsonPropertyName("end")]
    public readonly OpenGraphEdgeNode End;

    [JsonPropertyName("properties")]
    public readonly SortedDictionary<string, object> Properties = [];

    public OpenGraphEdge(OpenGraphNode start, OpenGraphNode end, string kind)
    {
        Start = start.ToEdgeNode();
        End = end.ToEdgeNode();
        Kind = kind;
    }

    public OpenGraphEdge(OpenGraphEdgeNode start, OpenGraphNode end, string kind)
    {
        Start = start;
        End = end.ToEdgeNode();
        Kind = kind;
    }

    public OpenGraphEdge(OpenGraphNode start, OpenGraphEdgeNode end, string kind)
    {
        Start = start.ToEdgeNode();
        End = end;
        Kind = kind;
    }

    public OpenGraphEdge(OpenGraphEdgeNode start, OpenGraphEdgeNode end, string kind)
    {
        Start = start;
        End = end;
        Kind = kind;
    }

    public void SetProperty(string name, object? value)
    {
        if (value is not null)
        {
            // Do not write empty values to the hash table / JSON
            Properties[name] = value;
        }
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
}
