using System.ComponentModel.DataAnnotations;

namespace ApartmentsAndManagement.Api.Models;

public class SearchFilter
{
    [Required] public string Keyword { get; set; }

    public List<string> Markets { get; set; } = new();

    [Range(1, 100)] public int Limit { get; set; } = 25;
}