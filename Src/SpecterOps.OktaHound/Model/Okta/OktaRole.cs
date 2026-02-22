using System.Text.Json.Serialization;

namespace SpecterOps.OktaHound.Model.Okta;

/// <summary>
/// Base class for Okta roles (both built-in and custom).
/// </summary>
internal abstract class OktaRole(string id, string domainName, string nodeKind) : OktaNode(id, domainName, nodeKind)
{
    private const string PermissionsPropertyName = "permissions";

    [JsonIgnore]
    public List<string>? Permissions
    {
        get => GetProperty<List<string>>(PermissionsPropertyName);
        set => SetProperty(PermissionsPropertyName, value);
    }

    [JsonIgnore]
    public abstract bool IsBuiltIn { get; }
}
