using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaResourceSet : OktaNode
{
    public const string NodeKind = "Okta_ResourceSet";
    public const string ContainsEdgeKind = "Okta_ResourceSetContains";
    public const string WorkflowsResourceSetId = "WORKFLOWS_IAM_POLICY";

    [JsonIgnore]
    private readonly string _originalId;

    /// <summary>
    /// The original identifier of the resource set as defined in Okta.
    /// </summary>
    [JsonIgnore]
    public override string OriginalId => _originalId;

    public OktaResourceSet(ResourceSet resourceSet, string domainName) : base(MakeResourceSetIdUnique(resourceSet.Id, domainName), domainName, NodeKind)
    {
        _originalId = resourceSet.Id;
        Name = resourceSet.Label;
        DisplayName = resourceSet.Label;

        SetProperty("created", resourceSet.Created);
        SetProperty("lastUpdated", resourceSet.LastUpdated);
        SetProperty("description", resourceSet.Description);
    }

    internal static string MakeResourceSetIdUnique(string resourceSetId, string domainName)
    {
        // The Workflows Resource Set is a built-in resource set with the WORKFLOWS_IAM_POLICY identifier that is shared across all Okta tenants.
        return resourceSetId == WorkflowsResourceSetId ? $"{WorkflowsResourceSetId}@{domainName}" : resourceSetId;
    }
}
