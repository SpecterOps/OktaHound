using Okta.Sdk.Model;
using System.Text.Json;

namespace SpecterOps.OktaHound.Database;

public sealed class OktaAuthorizationServer : OktaEntity
{
    public const string NodeKind = "Okta_AuthorizationServer";

    public DateTimeOffset? Created { get; set; }
    public DateTimeOffset? LastUpdated { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Issuer { get; set; }
    public string? IssuerMode { get; set; }
    public List<string>? Audiences { get; set; }

    protected override string[] Kinds => [NodeKind];

    private OktaAuthorizationServer() : base(string.Empty, string.Empty, string.Empty)
    {
    }

    public OktaAuthorizationServer(AuthorizationServer server, string domainName) : base(server.Id, server.Name, domainName)
    {
        DisplayName = server.Name;
        Created = server.Created;
        LastUpdated = server.LastUpdated;
        Description = server.Description;
        Status = server.Status?.Value;
        Issuer = server.Issuer;
        IssuerMode = server.IssuerMode;
        Audiences = server.Audiences;
        // TODO: Parse server.Credentials
    }

    protected override void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OktaAuthorizationServer);
    }

    // public static new OpenGraphEdgeNode CreateEdgeNode(string id) => new(id, NodeKind);
}
