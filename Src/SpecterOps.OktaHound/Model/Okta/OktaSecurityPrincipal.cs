using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.Okta;

/// <summary>
/// Base class for role-assignable resources in Okta (Users, Groups, and Apps).
/// </summary>
internal abstract class OktaSecurityPrincipal : OktaNode
{
    private const string HasRoleAssignmentsPropertyName = "hasRoleAssignments";

    [JsonIgnore()]
    public bool HasRoleAssignments
    {
        get => GetPropertyAsBool(HasRoleAssignmentsPropertyName) ?? false;
        set => SetProperty(HasRoleAssignmentsPropertyName, value);
    }

    [JsonIgnore()]
    public bool IsSuperAdmin
    {
        get;
        set;
    }

    [JsonIgnore()]
    public bool IsOrgAdmin
    {
        get;
        set;
    }

    protected OktaSecurityPrincipal(string id, OktaOrganization organization, string kind) : base(id, organization, kind)
    {
        // Push default property values into the model
        HasRoleAssignments = false;
    }
}
