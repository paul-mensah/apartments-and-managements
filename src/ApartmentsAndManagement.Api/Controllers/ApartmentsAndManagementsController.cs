using System.Net.Mime;
using ApartmentsAndManagement.Api.Models;
using ApartmentsAndManagement.Api.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentsAndManagement.Api.Controllers;

[ApiController]
[Route("api/apartments")]
public class ApartmentsAndManagementsController : ControllerBase
{
    private readonly ISearchService _searchService;

    public ApartmentsAndManagementsController(ISearchService searchService)
    {
        _searchService = searchService;
    }

    /// <summary>
    ///     Search for apartments
    /// </summary>
    /// <param name="filter"></param>
    /// <returns></returns>
    [HttpGet]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(BaseResponse<EmptyResponse>))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(BaseResponse<List<ApartmentsAndManagementsSearchResponse>>))]
    [ProducesResponseType(StatusCodes.Status424FailedDependency, Type = typeof(BaseResponse<EmptyResponse>))]
    public async Task<IActionResult> Search([FromQuery] SearchFilter filter)
    {
        var response = await _searchService.Search(filter);
        return StatusCode(response.Code, response);
    }
}