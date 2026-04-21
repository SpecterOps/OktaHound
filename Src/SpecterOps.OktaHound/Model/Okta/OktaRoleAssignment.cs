using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.Okta;

internal class OktaRoleAssignment : OktaNode
{
    public const string NodeKind = "Okta_RoleAssignment";
    public const string HasRoleAssignmentEdgeKind = "Okta_HasRoleAssignment";
    public const string ScopedToEdgeKind = "Okta_ScopedTo";
    private const string RoleTypePropertyName = "type";

    /// <summary>
    /// Cached role for this role assignment.
    /// </summary>
    /// <remarks>Speeds-up post-processing</remarks>
    [JsonIgnore]
    public OktaRole Role
    {
        get;
        private set;
    }

    /// <summary>
    /// Cached assignee node for this role assignment.
    /// </summary>
    /// <remarks>Speeds-up post-processing</remarks>
    [JsonIgnore]
    public OktaSecurityPrincipal Assignee
    {
        get;
        private set;
    }

    /// <summary>
    /// Cached target nodes for this role assignment.
    /// </summary>
    /// <remarks>Speeds-up post-processing</remarks>
    [JsonIgnore]
    public List<OktaNode> Targets
    {
        get;
        private set;
    } = [];

    [JsonIgnore]
    public string RoleType => GetProperty<string>(RoleTypePropertyName) ?? throw new ArgumentNullException(RoleTypePropertyName);

    public OktaRoleAssignment(StandardRole roleAssignment, OktaRole role, OktaSecurityPrincipal assignee, OktaOrganization organization) : base(DeriveRoleAssignmentId(roleAssignment, assignee.Id), organization, NodeKind)
    {
        Name = roleAssignment.Label;
        DisplayName = roleAssignment.Label;
        Assignee = assignee;
        Role = role;

        SetProperty(RoleTypePropertyName, roleAssignment.Type?.Value);
        SetProperty("status", roleAssignment.Status?.Value);
        SetProperty("assignmentType", roleAssignment.AssignmentType?.Value);
        SetProperty("created", roleAssignment.Created);
        SetProperty("lastUpdated", roleAssignment.LastUpdated);
    }

    public OktaRoleAssignment(CustomRole roleAssignment, OktaRole role, OktaSecurityPrincipal assignee, OktaOrganization organization) : base(DeriveRoleAssignmentId(roleAssignment, assignee.Id), organization, NodeKind)
    {
        Name = roleAssignment.Label;
        DisplayName = roleAssignment.Label;
        Assignee = assignee;
        Role = role;

        SetProperty(RoleTypePropertyName, roleAssignment.Type?.Value);
        SetProperty("status", roleAssignment.Status?.Value);
        SetProperty("assignmentType", roleAssignment.AssignmentType?.Value);
        SetProperty("created", roleAssignment.Created);
        SetProperty("lastUpdated", roleAssignment.LastUpdated);
    }

    public static OpenGraphEdgeNode CreateEdgeNode(string roleAssignmentId, string assigneeId) => new(DeriveRoleAssignmentId(roleAssignmentId, assigneeId), NodeKind);


    private static string DeriveRoleAssignmentId(StandardRole roleAssignment, string assigneeId)
    {
        return DeriveRoleAssignmentId(roleAssignment.Id, assigneeId);
    }

    private static string DeriveRoleAssignmentId(CustomRole roleAssignment, string assigneeId)
    {
        return DeriveRoleAssignmentId(roleAssignment.Id, assigneeId);
    }

    public static string DeriveRoleAssignmentId(string roleAssignmentId, string assigneeId)
    {
        // Role assignments may have non-unique IDs across different assignees
        return $"{roleAssignmentId}_{assigneeId}";
    }
}
