using System.Text.Json.Serialization;
using Okta.Sdk.Model;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.ActiveDirectory;

internal sealed class ActiveDirectoryUser : OpenGraphNode
{
    private const string NodeKind = "User";
    private const string ObjectSidPropertyName = "objectsid";
    private const string ObjectGuidPropertyName = "objectguid";

    [JsonIgnore]
    public string? ObjectSid => GetProperty<string>(ObjectSidPropertyName);

    [JsonIgnore]
    public string ObjectGuid => GetProperty<string>(ObjectGuidPropertyName) ?? throw new ArgumentException("The AD user does not have objectGuid.");

    [JsonIgnore]
    public string? DomainSid
    {
        get
        {
            if (ObjectSid is null)
            {
                // SID is typically not available for Okta->AD synced users.
                return null;
            }

            // Cut off the RID from the SID, e.g., S-1-5-21-2697957641-2271029196-387917394-500
            var ridSeparatorIndex = ObjectSid.LastIndexOf('-');
            return ObjectSid[..ridSeparatorIndex];
        }
    }

    public ActiveDirectoryUser(AppUser user, string? domainName) : base(GetUserId(user), [NodeKind])
    {
        // userPrincipalName or samAccountName
        var upn = GetProfileProperty(user, "userName");
        var samAccountName = GetProfileProperty(user, "samAccountName");

        if (upn is null && samAccountName is not null && domainName is not null)
        {
            // Construct the UPN, e.g., john@contoso.com
            upn = $"{samAccountName}@{domainName}";
        }

        Name = upn;
        DisplayName = GetProfileProperty(user, "displayName");

        // TODO: Sync these properties with names used by SharpHound
        SetProperty("samaccountname", samAccountName);
        SetProperty("userprincipalname", upn);
        SetProperty(ObjectGuidPropertyName, ActiveDirectoryDomain.ParseObjectGuid(user.ExternalId));
        SetProperty(ObjectSidPropertyName, GetProfileProperty(user, "objectSid"));
        SetProperty("distinguishedname", GetProfileProperty(user, "dn"));
        SetProperty("cn", GetProfileProperty(user, "cn"));
        SetProperty("givenname", GetProfileProperty(user, "firstName"));
        SetProperty("sn", GetProfileProperty(user, "lastName"));
        SetProperty("employeeid", GetProfileProperty(user, "employeeID"));
        SetProperty("employeenumber", GetProfileProperty(user, "employeeNumber"));
        SetProperty("primarygroupid", GetProfileProperty(user, "primaryGroupId"));
        SetProperty("department", GetProfileProperty(user, "department"));
        SetProperty("description", GetProfileProperty(user, "description"));
        SetProperty("city", GetProfileProperty(user, "city"));
        SetProperty("title", GetProfileProperty(user, "title"));
        SetProperty("email", GetProfileProperty(user, "email"));
    }

    /// <summary>
    /// Creates an OpenGraphEdgeNode representing the Kerberos SSO relationship between Okta and Active Directory.
    /// </summary>
    /// <param name="oktaDomain">The Okta domain.</param>
    /// <param name="activeDirectoryDomain">The Active Directory domain.</param>
    /// <param name="dnsCheck">Whether to perform a DNS check to validate the SPN.</param>
    /// <returns>An OpenGraphEdgeNode if the SPN is valid; otherwise, null.</returns>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public static OpenGraphEdgeNode? CreateKerberosEdgeNode(string oktaDomain, string activeDirectoryDomain, bool dnsCheck = true)
    {
        ArgumentNullException.ThrowIfNull(oktaDomain);
        ArgumentNullException.ThrowIfNull(activeDirectoryDomain);

        // Translate the Okta domain to SPN, e.g., from contoso.okta.com to HTTP/contoso.kerberos.okta.com
        // Possible Okta domain suffixes: okta.com, oktapreview.com, okta-emea.com, okta-gov.com, okta.mil, okta-miltest.com, trex-govcloud.com
        string[] oktaDomainParts = oktaDomain.Split('.');

        if (oktaDomainParts.Length < 2)
        {
            throw new ArgumentException("Invalid Okta domain format.", nameof(oktaDomain));
        }

        string oktaChildDomain = oktaDomainParts[0];
        string oktaParentDomain = string.Join('.', oktaDomainParts[^2..]);
        string oktaKerberosDomain = $"{oktaChildDomain}.kerberos.{oktaParentDomain}".ToLowerInvariant();
        string servicePrincipalName = $"HTTP/{oktaKerberosDomain}";

        if (dnsCheck)
        {
            try
            {
                // Perform a DNS lookup to check if the SPN is valid, to confirm the SSO feature is enabled in the tenant.
                // This is a heuristic to avoid creating meaningless edges.
                _ = System.Net.Dns.GetHostEntry(oktaKerberosDomain);
            }
            catch (Exception)
            {
                // DNS lookup failed, likely indicating that the SPN does not exist. Skip creating the edge.
                return null;
            }
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "serviceprincipalnames", servicePrincipalName }, // Example: HTTP/contoso.kerberos.okta.com
            { "domain", activeDirectoryDomain.ToUpperInvariant() } // Example: CONTOSO.COM
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }

    private static string GetUserId(AppUser user)
    {
        // Try to get the SID. This only works for inbound users.
        string? sid = GetProfileProperty(user, "objectSid");

        // Use objectGuid as a fallback for outbound synced users.
        // TODO: BloodHound currently does not support AD user matching by objectGuid.
        return sid ?? ActiveDirectoryDomain.ParseObjectGuid(user.ExternalId) ?? throw new ArgumentException("The AD account does not have any usable unique identifier.");
    }

    private static string? GetProfileProperty(AppUser user, string propertyName)
    {
        if (user.Profile is null)
        {
            return null;
        }

        var success = user.Profile.TryGetValue(propertyName, out var valueObj);
        return valueObj as string;
    }
}
