using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Storage;

public sealed class Property
{
    [JsonProperty("propertyID", NullValueHandling = NullValueHandling.Ignore)]
    public string Id { get; set; }

    public string Name { get; set; }
    public string FormerName { get; set; }
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public string Market { get; set; }
    public string State { get; set; }

    [JsonProperty("lat", NullValueHandling = NullValueHandling.Ignore)]
    public double? Latitude { get; set; }

    [JsonProperty("lng", NullValueHandling = NullValueHandling.Ignore)]
    public double? Longitude { get; set; }
}