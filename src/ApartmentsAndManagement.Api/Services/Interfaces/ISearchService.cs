using ApartmentsAndManagement.Api.Models;

namespace ApartmentsAndManagement.Api.Services.Interfaces;

public interface ISearchService
{
    Task<BaseResponse<List<ApartmentsAndManagementsSearchResponse>>> Search(SearchFilter filter);
}