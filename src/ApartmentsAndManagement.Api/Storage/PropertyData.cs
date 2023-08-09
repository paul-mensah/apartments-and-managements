using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Storage;

public class PropertyData
{
    [JsonProperty("property")] public Property Property { get; set; }
}