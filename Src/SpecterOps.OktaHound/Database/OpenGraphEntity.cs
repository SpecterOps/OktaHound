using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace SpecterOps.OktaHound.Database;

[PrimaryKey(nameof(Id))]
public abstract class OpenGraphEntity(string id, string name)
{
    [JsonIgnore]
    public string Id { get; set; } = id;

    public string Name { get; set; } = name;

    public string? DisplayName { get; set; }

    [JsonIgnore]
    [NotMapped]
    protected abstract string[] Kinds { get; }

    public void Serialize(Utf8JsonWriter writer)
    {
        writer.WriteStartObject();

        writer.WriteString("id", Id);

        // Nest all object properties
        writer.WritePropertyName("properties");
        SerializeProperties(writer);

        writer.WriteStartArray("kinds");
        foreach (string kind in Kinds)
        {
            writer.WriteStringValue(kind);
        }
        writer.WriteEndArray();

        writer.WriteEndObject();
        writer.Flush();
    }

    protected virtual void SerializeProperties(Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize(writer, this, TestSerializationContext.Default.OpenGraphEntity);
    }
}
