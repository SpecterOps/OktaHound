using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal abstract class OpenGraphBase<ElementsType>(JsonSerializerContext serializationContext, ElementsType elements, string? sourceKind = null) where ElementsType : OpenGraphElements

{
    // OpenGraph schema for manual output validation from https://github.com/SpecterOps/chow
    private const string SchemaUrl = "https://raw.githubusercontent.com/SpecterOps/chow/refs/heads/main/pkg/validator/jsonschema/opengraph.json";

    [JsonPropertyName("$schema")]
    public readonly string Schema = SchemaUrl;

    [JsonPropertyName("metadata")]
    public readonly OpenGraphMetadata Metadata = new(sourceKind);

    [JsonPropertyName("graph")]
    public readonly ElementsType Elements = elements;

    [JsonIgnore]
    public int NodeCount => Elements.NodeCount;

    [JsonIgnore]
    public int EdgeCount => Elements.EdgeCount;

    [JsonIgnore]
    private readonly JsonSerializerContext SerializationContext = serializationContext;

    public void AddNode(OpenGraphNode node) => Elements.GenericNodes.Add(node);

    public void AddEdge(OpenGraphEdge edge)
    {
        // Create the edge collection for this kind if it doesn't exist, then add the edge.
        Elements.EdgesByKind.GetOrAdd(edge.Kind, _ => []).Add(edge);
    }

    public OpenGraphEdge AddEdge(OpenGraphNode start, OpenGraphNode end, string kind)
    {
        OpenGraphEdge edge = new(start, end, kind);
        AddEdge(edge);
        return edge;
    }

    public OpenGraphEdge AddEdge(OpenGraphEdgeNode start, OpenGraphNode end, string kind)
    {
        OpenGraphEdge edge = new(start, end, kind);
        AddEdge(edge);
        return edge;
    }

    public OpenGraphEdge AddEdge(OpenGraphNode start, OpenGraphEdgeNode end, string kind)
    {
        OpenGraphEdge edge = new(start, end, kind);
        AddEdge(edge);
        return edge;
    }

    public OpenGraphEdge AddEdge(OpenGraphEdgeNode start, OpenGraphEdgeNode end, string kind)
    {
        OpenGraphEdge edge = new(start, end, kind);
        AddEdge(edge);
        return edge;
    }

    public void SaveAsJson(string path)
    {
        using var stream = File.Open(path, FileMode.Create);
        using var writer = new Utf8JsonWriter(stream, new() { Indented = true });

        JsonSerializer.Serialize(writer, this, typeof(OpenGraphBase<ElementsType>), SerializationContext);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, typeof(OpenGraphBase<ElementsType>), SerializationContext);
    }
}
