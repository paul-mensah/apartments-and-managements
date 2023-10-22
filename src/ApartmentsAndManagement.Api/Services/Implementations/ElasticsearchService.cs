using Akka.Actor;
using Akka.Util.Internal;
using ApartmentsAndManagement.Api.Actors;
using ApartmentsAndManagement.Api.Actors.Messages;
using ApartmentsAndManagement.Api.Configurations;
using ApartmentsAndManagement.Api.Models;
using ApartmentsAndManagement.Api.Services.Interfaces;
using ApartmentsAndManagement.Api.Storage;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;

namespace ApartmentsAndManagement.Api.Services.Implementations;

public class ElasticsearchService : IElasticsearchService
{
    private readonly IElasticClient _elasticClient;
    private readonly ElasticsearchConfig _elasticsearchConfig;
    private readonly ILogger<ElasticsearchService> _logger;

    public ElasticsearchService(ILogger<ElasticsearchService> logger,
        IElasticClient elasticClient,
        IOptions<ElasticsearchConfig> elasticsearchConfig)
    {
        _logger = logger;
        _elasticClient = elasticClient;
        _elasticsearchConfig = elasticsearchConfig.Value;
    }

    public async Task<CreateIndexResponse> CreateIndex<T>(string index) where T : class
    {
        var indexConfig = new Func<IndexSettingsDescriptor, IPromise<IIndexSettings>>(settings => settings
            .Analysis(analysis => analysis
                .Analyzers(analyzers => analyzers
                    .Standard("standard_english", desc => desc
                        .StopWords("_english_")))
                .TokenFilters(tokenFilter => tokenFilter
                    .Stop("stop", desc => desc
                        .StopWords("_english_")))));

        return await _elasticClient.Indices.CreateAsync(index, selector => selector
            .Map(descriptor => descriptor
                .AutoMap<T>())
            .Settings(indexConfig));
    }

    public async Task<BulkResponse> IndexBulkDocuments<T>(string index, IEnumerable<T> documents) where T : class
    {
        return await _elasticClient.BulkAsync(new BulkRequest(index)
        {
            Operations = documents
                .Select(doc => new BulkIndexOperation<T>(doc))
                .Cast<IBulkOperation>()
                .ToList()
        });
    }
    
    public async Task<ISearchResponse<dynamic>> SearchAsync(SearchFilter filter)
    {
        QueryBase searchQuery = new SimpleQueryStringQuery
        {
            Query = filter.Keyword,
            Fields = new Field[]
            {
                new("property.name"),
                new("property.formerName"),
                new("property.streetAddress"),
                new("property.city"),
                new("management.name"),
                new("management.city")
            }
        };

        if (filter.Markets.Any())
            searchQuery = searchQuery && new MultiMatchQuery
            {
                Fields = new[]
                {
                    new Field("management.market"),
                    new Field("property.market")
                },
                Query = string.Join(" ", filter.Markets),
                Operator = Operator.Or
            };

        var searchResponse = await _elasticClient.SearchAsync<SearchDocument>(descriptor => descriptor
            .Index($"{_elasticsearchConfig.ManagementIndex}, {_elasticsearchConfig.PropertiesIndex}")
            .Query(q => searchQuery)
            .Size(filter.Limit));

        if (!searchResponse.IsValid)
            _logger.LogError(searchResponse.OriginalException,
                "An error occured searching for documents\n{debugInformation}", searchResponse.DebugInformation);

        return searchResponse;
    }
    
    public async Task IndexDocuments()
    {
        await IndexData<PropertyData>(_elasticsearchConfig.PropertiesIndex);
        await IndexData<ManagementData>(_elasticsearchConfig.ManagementIndex);
    }

    private async Task IndexData<T>(string index) where T : class
    {
        CreateIndexResponse createIndexResponse = await CreateIndex<T>(index);

        const string indexAlreadyCreatedMessage = "resource_already_exists_exception";
        if (!createIndexResponse.IsValid &&
            !indexAlreadyCreatedMessage.Equals(createIndexResponse.ServerError.Error.Type,
                StringComparison.OrdinalIgnoreCase))
            _logger.LogError(createIndexResponse.OriginalException, "An error occured creating index: {index}", index);

        string fileName = typeof(PropertyData) == typeof(T)
            ? "props.json"
            : "mgmt.json";

        await ReadFileAndSave<T>(fileName);
    }

    private static async Task ReadFileAndSave<T>(string fileName)
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