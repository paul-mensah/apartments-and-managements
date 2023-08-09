using ApartmentsAndManagement.Api.Configurations;
using ApartmentsAndManagement.Api.Models;
using ApartmentsAndManagement.Api.Services.Interfaces;
using ApartmentsAndManagement.Api.Storage;
using Microsoft.Extensions.Options;
using Nest;

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
}