using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal sealed class OktaCustomRole : OktaRole
{
    public const string NodeKind = "Okta_CustomRole";
    public const string HasCustomRoleEdgeKind = "Okta_HasRole";
    public const string ResetPasswordEdgeKind = "Okta_ResetPassword";
    public const string ResetFactorsEdgeKind = "Okta_ResetFactors";
    public const string AddMemberEdgeKind = "Okta_AddMember";
    public const string ManageAppEdgeKind = "Okta_ManageApp";
    public const string ReadClientSecretEdgeKind = "Okta_ReadClientSecret";

    public override bool IsBuiltIn => false;

    public OktaCustomRole(IamRole role, OktaOrganization organization) : base(role.Id, organization, NodeKind)
    {
        Name = role.Label;
        DisplayName = role.Label;

        SetProperty("created", role.Created);
        SetProperty("lastUpdated", role.LastUpdated);
    }

    public static new OpenGraphEdgeNode CreateEdgeNode(string customRoleId) => new(customRoleId, NodeKind);
}
