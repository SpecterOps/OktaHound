using System.Text.Json;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaCustomRole : OktaRole
{
    public const string NodeKind = "Okta_CustomRole";
    public const string HasCustomRoleEdgeKind = "Okta_HasRole";
    public const string ResetPasswordEdgeKind = "Okta_ResetPassword";
    public const string ResetFactorsEdgeKind = "Okta_ResetFactors";
    public const string AddMemberEdgeKind = "Okta_AddMember";
    public const string ManageAppEdgeKind = "Okta_ManageApp";

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }

    public override bool IsBuiltIn => false;

    protected override string[] Kinds => [NodeKind];

    private OktaCustomRole() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaCustomRole(IamRole role, string domainName) : base(role.Id, role.Label, domainName)
    {
        DisplayName = role.Label;
        Created = role.Created;
        LastUpdated = role.LastUpdated;
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaCustomRole);
    }
}
