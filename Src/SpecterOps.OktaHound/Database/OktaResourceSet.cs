using System.Text.Json.Serialization;
using System.Text.Json;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaResourceSet : OktaEntity
{
    public const string NodeKind = "Okta_ResourceSet";
    public const string ContainsEdgeKind = "Okta_ResourceSetContains";
    public const string WorkflowsResourceSetId = "WORKFLOWS_IAM_POLICY";

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public string? Description { get; set; }

    /// <summary>
    /// The original identifier of the resource set as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public string OriginalId { get; private set; } = string.Empty;

    protected override string[] Kinds => [NodeKind];

    private OktaResourceSet() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaResourceSet(ResourceSet resourceSet, string domainName) : base(MakeResourceSetIdUnique(resourceSet.Id, domainName), resourceSet.Label, domainName)
    {
        OriginalId = resourceSet.Id;
        DisplayName = resourceSet.Label;
        Created = resourceSet.Created;
        LastUpdated = resourceSet.LastUpdated;
        Description = resourceSet.Description;
    }

    internal static string MakeResourceSetIdUnique(string resourceSetId, string domainName)
    {
        // The Workflows Resource Set is a built-in resource set with the WORKFLOWS_IAM_POLICY identifier that is shared across all Okta tenants.
        return resourceSetId == WorkflowsResourceSetId ? $"{WorkflowsResourceSetId}@{domainName}" : resourceSetId;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaResourceSet);
    }
}
