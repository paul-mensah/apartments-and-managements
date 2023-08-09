namespace ApartmentsAndManagement.Api.Models;

public sealed class ApartmentsAndManagementsSearchResponse
{
    public string Type { get; set; }
    public string Id { get; set; }
    public string Name { get; set; }
    public string Market { get; set; }
    public string State { get; set; }
    public string FormerName { get; set; }
    public string StreetAddress { get; set; }
    public string City { get; set; }
    public float? Latitude { get; set; }
    public float? Longitude { get; set; }
}