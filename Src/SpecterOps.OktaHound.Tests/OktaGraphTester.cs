using SpecterOps.OktaHound.Model.Okta;

namespace SpecterOps.OktaHound.Tests;

[TestClass]
public class OktaGraphTester
{
    [TestMethod]
    public void OktaGraph_Serialization_Empty()
    {
        OktaOrganization org = new(id: "abc", domainName: "contoso.okta.com", companyName: "Contoso");
        OktaGraph graph = new(org);
        string json = graph.ToString();
        Assert.AreEqual("{\"$schema\":\"https://raw.githubusercontent.com/SpecterOps/chow/refs/heads/main/pkg/validator/jsonschema/opengraph.json\",\"metadata\":{\"source_kind\":\"Okta\"},\"graph\":{\"nodes\":[{\"id\":\"abc\",\"properties\":{\"displayName\":\"Contoso\",\"name\":\"contoso.okta.com\",\"oktaDomain\":\"contoso.okta.com\"},\"kinds\":[\"Okta_Organization\"]}],\"edges\":[]}}", json);
    }
}
