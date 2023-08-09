using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Storage;

public class Management
{
    [JsonProperty("mgmtID", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    public string Name { get; set; }
    public string Market { get; set; }
    public string State { get; set; }
}