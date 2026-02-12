using System.Collections.Concurrent;
using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.OpenGraph;

internal class OpenGraphElements
{
    /// <summary>
    /// Gets an enumerable collection of all nodes in the graph.
    /// </summary>
    /// <remarks>
    /// This property is only used for JSON serialization.
    /// </remarks>
    [JsonPropertyName("nodes")]
    public IEnumerable<OpenGraphNode> Nodes => EnumerateNodes;

    [JsonIgnore]
    protected virtual IEnumerable<OpenGraphNode> EnumerateNodes => GenericNodes;

    /// <summary>
    /// Gets an enumerable collection of all edges in the graph.
    /// </summary>
    /// <remarks>
    /// This property is only used for JSON serialization.
    /// </remarks>
    [JsonPropertyName("edges")]
    public IEnumerable<OpenGraphEdge> Edges => EdgesByKind.SelectMany(item => item.Value);

    [JsonIgnore]
    public virtual int NodeCount => GenericNodes.Count;

    [JsonIgnore]
    public int EdgeCount => EdgesByKind.Sum(item => item.Value.Count);

    /// <summary>
    /// Represents a thread-safe collection of additional graph nodes.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentBag<OpenGraphNode> GenericNodes = [];

    /// <summary>
    /// Represents a thread-safe collection that maps edge kinds to their corresponding collections of <see
    /// cref="OpenGraphEdge"/> objects.
    /// </summary>
    [JsonIgnore]
    public readonly ConcurrentDictionary<string, ConcurrentBag<OpenGraphEdge>> EdgesByKind = new();
}
