using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Okta.Sdk.Model;

namespace SpecterOps.OktaHound.Database;

public class OktaRoleAssignment : OktaEntity
{
    public const string NodeKind = "Okta_RoleAssignment";
    public const string HasRoleAssignmentEdgeKind = "Okta_HasRoleAssignment";
    public const string ScopedToEdgeKind = "Okta_ScopedTo";
    public string RoleType { get; set; }
    public string? Status { get; set; }
    public string? AssignmentType { get; set; }
    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaRoleAssignment() : base(string.Empty, string.Empty, string.Empty)
    {
        RoleType = string.Empty;
        Role = null!;
        Assignee = null!;
    }

    /// <summary>
    /// Cached role for this role assignment.
    /// </summary>
    /// <remarks>Speeds-up post-processing</remarks>
    [JsonIgnore]
    [NotMapped]
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
    [NotMapped]
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
    [NotMapped]
    public List<OktaEntity> Targets
    {
        get;
        private set;
    } = [];

    public OktaRoleAssignment(StandardRole roleAssignment, OktaRole role, OktaSecurityPrincipal assignee, string domainName) : base(DeriveRoleAssignmentId(roleAssignment, assignee.Id), roleAssignment.Label, domainName)
    {
        DisplayName = roleAssignment.Label;
        Assignee = assignee;
        Role = role;

        RoleType = roleAssignment.Type.Value;
        Status = roleAssignment.Status.Value;
        AssignmentType = roleAssignment.AssignmentType.Value;
        Created = roleAssignment.Created;
        LastUpdated = roleAssignment.LastUpdated;
    }

    public OktaRoleAssignment(CustomRole roleAssignment, OktaRole role, OktaSecurityPrincipal assignee, string domainName) : base(DeriveRoleAssignmentId(roleAssignment, assignee.Id), roleAssignment.Label, domainName)
    {
        DisplayName = roleAssignment.Label;
        Assignee = assignee;
        Role = role;

        RoleType = roleAssignment.Type.Value;
        Status = roleAssignment.Status.Value;
        AssignmentType = roleAssignment.AssignmentType.Value;
        Created = roleAssignment.Created;
        LastUpdated = roleAssignment.LastUpdated;
    }

    private static string DeriveRoleAssignmentId(StandardRole roleAssignment, string assigneeId)
    {
        return DeriveRoleAssignmentId(roleAssignment.Id, assigneeId);
    }

    private static string DeriveRoleAssignmentId(CustomRole roleAssignment, string assigneeId)
    {
        return DeriveRoleAssignmentId(roleAssignment.Id, assigneeId);
    }

    private static string DeriveRoleAssignmentId(string roleAssignmentId, string assigneeId)
    {
        // Role assignments may have non-unique IDs across different assignees
        return $"{roleAssignmentId}_{assigneeId}";
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaRoleAssignment);
    }
}
