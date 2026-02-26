using System.Text.Json;
using SpecterOps.OktaHound.Model.ActiveDirectory;
using SpecterOps.OktaHound.Model.OpenGraph;

namespace SpecterOps.OktaHound.Tests;

[TestClass]
public class ActiveDirectoryUserTester
{
    [TestMethod]
    public void ActiveDirectoryUser_CreateKerberosEdgeNode_DnsCheckFalse_Serialize()
    {
        var edgeNode = ActiveDirectoryUser.CreateKerberosEdgeNode("contoso.okta.com", "contoso.com", dnsCheck: false);
        var expectedJson = "{\"properties\":{\"serviceprincipalnames\":\"HTTP/contoso.kerberos.okta.com\",\"domain\":\"CONTOSO.COM\"},\"match_by\":\"properties\",\"kind\":\"User\"}";

        string json = JsonSerializer.Serialize(edgeNode, typeof(OpenGraphEdgeNode), OpenGraphSerializationContext.Default);

        Assert.AreEqual(expectedJson, json);
    }

    [TestMethod]
    public void ActiveDirectoryUser_CreateKerberosEdgeNode_DnsCheckTrue_DummyDomain_ReturnsNull()
    {
        var edgeNode = ActiveDirectoryUser.CreateKerberosEdgeNode("this-domain-does-not-exist.okta.com", "contoso.com", dnsCheck: true);

        Assert.IsNull(edgeNode);
    }

    [TestMethod]
    public void ActiveDirectoryUser_CreateKerberosEdgeNode_DnsCheckTrue_RealDomainName_ReturnsNotNull()
    {
        var edgeNode = ActiveDirectoryUser.CreateKerberosEdgeNode("spectoropspreview.oktapreview.com", "contoso.com", dnsCheck: true);

        Assert.Inconclusive("The DNS resolution is not yet implemented properly.");
        Assert.IsNotNull(edgeNode);
    }
}
