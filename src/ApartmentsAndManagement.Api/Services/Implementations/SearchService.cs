using Akka.Actor;
using Akka.Util.Internal;
using ApartmentsAndManagement.Api.Actors;
using ApartmentsAndManagement.Api.Actors.Messages;
using ApartmentsAndManagement.Api.Configurations;
using ApartmentsAndManagement.Api.Models;
using ApartmentsAndManagement.Api.Services.Interfaces;
using ApartmentsAndManagement.Api.Storage;
using Mapster;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Services.Implementations;

public class SearchService : ISearchService
{
    private readonly ElasticsearchConfig _elasticSearchConfig;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IElasticsearchService elasticsearchService,
        IOptions<ElasticsearchConfig> elasticSearchConfig,
        ILogger<SearchService> logger)
    {
        _elasticsearchService = elasticsearchService;
        _logger = logger;
        _elasticSearchConfig = elasticSearchConfig.Value;
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

    public async Task IndexDocuments()
    {
        await IndexData<PropertyData>(_elasticSearchConfig.PropertiesIndex);
        await IndexData<ManagementData>(_elasticSearchConfig.ManagementIndex);
    }

    private async Task IndexData<T>(string index) where T : class
    {
        CreateIndexResponse createIndexResponse = await _elasticsearchService
            .CreateIndex<T>(index);

        const string indexAlreadyCreatedMessage = "resource_already_exists_exception";
        if (!createIndexResponse.IsValid &&
            !indexAlreadyCreatedMessage.Equals(createIndexResponse.ServerError.Error.Type,
                StringComparison.OrdinalIgnoreCase))
            _logger.LogError(createIndexResponse.OriginalException, "An error occured creating index: {index}", index);

        string fileName = typeof(PropertyData) == typeof(T)
            ? "props.json"
            : "mgmt.json";

        await ReadFileAndSave<T>(fileName, index);
    }

    private static async Task ReadFileAndSave<T>(string fileName, string indexName)
    {
        string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ElasticsearchData", fileName);

        if (File.Exists(fullPath))
        {
            string fileContentString = await File.ReadAllTextAsync(fullPath);

            if (typeof(T) == typeof(PropertyData))
            {
                var content = JsonConvert.DeserializeObject<List<PropertyData>>(fileContentString);

                content.Chunk(1000).ForEach(data =>
                {
                    ParentActor.ElasticsearchPersistenceActor
                        .Tell(new PropertiesPersistenceMessage(data.ToList()), ActorRefs.NoSender);
                });
            }
            else
            {
                var content = JsonConvert.DeserializeObject<List<ManagementData>>(fileContentString);

                content.Chunk(1000).ForEach(data =>
                {
                    ParentActor.ElasticsearchPersistenceActor
                        .Tell(new ManagementPersistenceMessage(data.ToList()), ActorRefs.NoSender);
                });
            }
        }
    }
}