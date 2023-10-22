using Akka.Util.Internal;
using ApartmentsAndManagement.Api.Models;
using ApartmentsAndManagement.Api.Services.Interfaces;
using ApartmentsAndManagement.Api.Storage;
using Mapster;
using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Services.Implementations;

public class SearchService : ISearchService
{
    private readonly IElasticsearchService _elasticsearchService;

    public SearchService(IElasticsearchService elasticsearchService)
    {
        _elasticsearchService = elasticsearchService;
    }

    public async Task<BaseResponse<List<ApartmentsAndManagementsSearchResponse>>> Search(SearchFilter filter)
    {
        var searchResponse = await _elasticsearchService.SearchAsync(filter);

        if (!searchResponse.IsValid)
            return new BaseResponse<List<ApartmentsAndManagementsSearchResponse>>
            {
                Code = StatusCodes.Status424FailedDependency,
                Message = "An error occured searching for properties and managements"
            };

        var documents = new List<ApartmentsAndManagementsSearchResponse>();

        searchResponse.Documents.ForEach(document =>
        {
            SearchDocument searchDocument =
                JsonConvert.DeserializeObject<SearchDocument>(JsonConvert.SerializeObject(document));
            ApartmentsAndManagementsSearchResponse response = searchDocument.Property != null
                ? searchDocument.Property.Adapt<ApartmentsAndManagementsSearchResponse>()
                : searchDocument.Management.Adapt<ApartmentsAndManagementsSearchResponse>();

            response.Type = searchDocument.Property != null ? "Property" : "Management";
            documents.Add(response);
        });

        return new BaseResponse<List<ApartmentsAndManagementsSearchResponse>>
        {
            Code = StatusCodes.Status200OK,
            Message = "Retrieved successfully " + searchResponse.Documents.Count,
            Data = documents
        };
    }
}