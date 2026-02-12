using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaCustomRole : OktaNode
{
    public const string NodeKind = "Okta_CustomRole";
    public const string HasCustomRoleEdgeKind = "Okta_HasRole";
    private const string PermissionsPropertyName = "permissions";

    [JsonIgnore]
    public List<string>? Permissions
    {
        get => GetProperty<List<string>>(PermissionsPropertyName);
        set => SetProperty(PermissionsPropertyName, value);
    }

    public OktaCustomRole(IamRole role, string domainName) : base(role.Id, domainName, NodeKind)
    {
        Name = role.Label;
        DisplayName = role.Label;

        SetProperty("created", role.Created);
        SetProperty("lastUpdated", role.LastUpdated);
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string customRoleId) => new(customRoleId, NodeKind);
}
