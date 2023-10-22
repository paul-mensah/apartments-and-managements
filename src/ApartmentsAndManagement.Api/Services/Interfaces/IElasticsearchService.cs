using ApartmentsAndManagement.Api.Models;
using Nest;

namespace ApartmentsAndManagement.Api.Services.Interfaces;

public interface IElasticsearchService
{
    Task<CreateIndexResponse> CreateIndex<T>(string index) where T : class;
    Task<BulkResponse> IndexBulkDocuments<T>(string index, IEnumerable<T> documents) where T : class;
    Task<ISearchResponse<dynamic>> SearchAsync(SearchFilter filter);
    Task IndexDocuments();
}