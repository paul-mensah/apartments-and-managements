using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Storage;

public class ManagementData
{
    [JsonProperty("mgmt")] public Management Management { get; set; }
}