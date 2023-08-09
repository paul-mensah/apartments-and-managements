using Akka.Actor;
using ApartmentsAndManagement.Api.Actors.Messages;
using ApartmentsAndManagement.Api.Configurations;
using ApartmentsAndManagement.Api.Services.Interfaces;
using Microsoft.Extensions.Options;
using Nest;

namespace ApartmentsAndManagement.Api.Actors;

public class ElasticsearchPersistenceActor : ReceiveActor
{
    private readonly ElasticsearchConfig _elasticsearchConfig;
    private readonly IElasticsearchService _elasticsearchService;
    private readonly ILogger<ElasticsearchPersistenceActor> _logger;

    public ElasticsearchPersistenceActor(ILogger<ElasticsearchPersistenceActor> logger,
        IElasticsearchService elasticsearchService,
        IOptions<ElasticsearchConfig> elasticsearchConfig)
    {
        _logger = logger;
        _elasticsearchService = elasticsearchService;
        _elasticsearchConfig = elasticsearchConfig.Value;

        ReceiveAsync<ManagementPersistenceMessage>(PersistManagementsToElasticsearch);
        ReceiveAsync<PropertiesPersistenceMessage>(PersistPropertiesDocuments);
    }

    private async Task PersistManagementsToElasticsearch(ManagementPersistenceMessage persistenceMessage)
    {
        try
        {
            BulkResponse response = await _elasticsearchService.IndexBulkDocuments(_elasticsearchConfig.ManagementIndex,
                persistenceMessage.Documents);

            if (!response.IsValid)
                _logger.LogError(response.OriginalException,
                    "An error occured persisting management documents\nDebugInformation: {debugInformation}",
                    response.DebugInformation);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured persisting management documents\nCount: {count}",
                persistenceMessage.Documents.Count);
        }
    }

    private async Task PersistPropertiesDocuments(PropertiesPersistenceMessage persistenceMessage)
    {
        try
        {
            BulkResponse response = await _elasticsearchService.IndexBulkDocuments(_elasticsearchConfig.PropertiesIndex,
                persistenceMessage.Documents);

            if (!response.IsValid)
                _logger.LogError(response.OriginalException,
                    "An error occured persisting property documents\nDebugInformation: {debugInformation}",
                    response.DebugInformation);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occured persisting property documents\nCount: {count}",
                persistenceMessage.Documents.Count);
        }
    }
}