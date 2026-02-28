namespace SpecterOps.OktaHound.Database;

public sealed class CollectionStatus
{
	public const int SingletonId = 1;

	public int Id { get; set; } = SingletonId;

	public bool OrganizationsCollected { get; set; }

	public bool UsersCollected { get; set; }

	public bool GroupsCollected { get; set; }

	public bool AgentPoolsCollected { get; set; }

	public bool AgentsCollected { get; set; }

	public bool DevicesCollected { get; set; }

	public bool ResourceSetsCollected { get; set; }

	public bool RealmsCollected { get; set; }

	public bool BuiltinRolesCollected { get; set; }

	public bool CustomRolesCollected { get; set; }

	public bool ApplicationsCollected { get; set; }

	public bool ApiTokensCollected { get; set; }

	public bool AuthorizationServersCollected { get; set; }

	public bool IdentityProvidersCollected { get; set; }

	public bool ApiServiceIntegrationsCollected { get; set; }

	public bool PoliciesCollected { get; set; }

	public bool RoleAssignmentsCollected { get; set; }

	public bool ClientSecretsCollected { get; set; }

	public bool JwksCollected { get; set; }
}
