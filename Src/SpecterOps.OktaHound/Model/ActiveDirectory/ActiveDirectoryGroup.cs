using SpecterOps.OktaHound.Model.Okta;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Model.ActiveDirectory;

internal sealed class ActiveDirectoryGroup : OpenGraphNode
{
    private const string NodeKind = "Group";

    public ActiveDirectoryGroup(OktaGroup group) :
        base(group.ObjectSid ?? throw new ArgumentException("Could not read group SID.", nameof(group)), [NodeKind])
    {
        Name = group.SamAccountName;
        DisplayName = group.DisplayName ?? group.SamAccountName;

        // TODO: Sync these properties with names used by SharpHound
        SetProperty("samaccountname", group.SamAccountName);
        SetProperty("objectguid", group.ObjectGuid);
        SetProperty("distinguishedname", group.DistinguishedName);
        SetProperty("domainsid", group.DomainSid);
    }

    public static OpenGraphEdgeNode? CreateEdgeNode(string groupName, string? domainFqdn)
    {
        if (groupName is null || domainFqdn is null)
        {
            return null;
        }

        OrderedDictionary<string, string> properties = new()
        {
            { "samAccountName", groupName }, // Example: IT
            { "domainFqdn", domainFqdn } // Example: contoso.com
        };

        return new OpenGraphEdgeNode(properties, NodeKind);
    }
}
