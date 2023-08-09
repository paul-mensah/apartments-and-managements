using System.Text.Json;
using ApartmentsAndManagement.Api.Services.Interfaces;
using ApartmentsAndManagement.Api.Middlewares;

namespace ApartmentsAndManagement.Api.Extensions;

public static class BuilderExtension
{
    public static WebApplication BuildApplication(this WebApplicationBuilder builder)
    {
        builder.Services.AddSwaggerDocumentation();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddCors();
        builder.Services.AddControllers().AddJsonOptions(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        builder.Services.Configure<RouteOptions>(o => o.LowercaseUrls = true);
        builder.Services.AddCustomServicesAndConfigurations(builder.Configuration);
        builder.Services.AddHealthChecks();

        return builder.Build();
    }

    private static async Task CreateIndicesAndSeedData(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            ISearchService searchService = scope.ServiceProvider.GetRequiredService<ISearchService>();

            await searchService.IndexDocuments();
        }
        catch (Exception e)
        {
            logger.LogError(e, "An error occured creating indices and seeding properties and management");
        }
    }

    public static void RunApplication(this WebApplication application)
    {
        CreateIndicesAndSeedData(application.Services).GetAwaiter().GetResult();

        // Configure the HTTP request pipeline.
        application.UseSwagger();
        application.UseSwaggerUI(s => { s.SwaggerEndpoint("/swagger/v1/swagger.json", "Apartments and Managements API"); });

        application.UseCors(x => x
            .AllowAnyMethod()
            .AllowAnyHeader()
            .SetIsOriginAllowed(origin => true)
            .AllowCredentials());

        application.UseRouting();
        application.ConfigureGlobalHandler(application.Logger);
        application.UseAuthorization();
        application.MapControllers();

        application.Run();
    }
}