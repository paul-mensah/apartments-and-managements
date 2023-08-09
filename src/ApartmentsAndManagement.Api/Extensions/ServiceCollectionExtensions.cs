using System.Reflection;
using Akka.Actor;
using Akka.DI.AutoFac;
using Akka.DI.Core;
using Akka.Routing;
using ApartmentsAndManagement.Api.Actors;
using ApartmentsAndManagement.Api.Configurations;
using ApartmentsAndManagement.Api.Services.Implementations;
using ApartmentsAndManagement.Api.Services.Interfaces;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Elasticsearch.Net;
using Microsoft.OpenApi.Models;
using Nest;

namespace ApartmentsAndManagement.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Apartments and Managements API",
                Version = "v1",
                Description = "Apartments and Managements API",
                Contact = new OpenApiContact
                {
                    Name = "Paul Mensah",
                    Email = "paulmensah1409@gmail.com"
                }
            });

            c.ResolveConflictingActions(resolver => resolver.First());
            c.EnableAnnotations();

            string xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            string xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);
        });
    }

    private static void AddElasticSearch(this IServiceCollection services,
        Action<ElasticsearchConfig> elasticsearchConfig)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.Configure(elasticsearchConfig);

        ElasticsearchConfig elasticsearchConfiguration = new();
        elasticsearchConfig.Invoke(elasticsearchConfiguration);

        SingleNodeConnectionPool pool = new(new Uri(elasticsearchConfiguration.Url));
        ConnectionSettings connectionSettings = new ConnectionSettings(pool)
            .PrettyJson()
            .DisableDirectStreaming()
            .EnableApiVersioningHeader();

        ElasticClient elasticClient = new(connectionSettings);
        ElasticLowLevelClient elasticLowLevelClient = new(connectionSettings);

        services.AddSingleton<IElasticClient>(elasticClient);
        services.AddSingleton<IElasticLowLevelClient>(elasticLowLevelClient);
        services.AddSingleton<IElasticsearchService, ElasticsearchService>();
    }

    private static void AddActorSystem(this IServiceCollection services, Action<ResizeActorConfig> resizeActorConfig)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        services.Configure(resizeActorConfig);

        ResizeActorConfig resizeActorConfiguration = new();
        resizeActorConfig.Invoke(resizeActorConfiguration);

        ActorSystem actorSystem = ActorSystem.Create("SmartApartmentDataActors");
        services.AddSingleton(_ => actorSystem);

        ContainerBuilder containerBuilder = new();
        containerBuilder.Populate(services);

        containerBuilder.RegisterType<ElasticsearchPersistenceActor>();

        IContainer container = containerBuilder.Build();
        AutoFacDependencyResolver _ = new(container, actorSystem);

        // Create child actors
        ParentActor.ActorSystem = actorSystem;

        ParentActor.ElasticsearchPersistenceActor = actorSystem.ActorOf(actorSystem.DI()
            .Props<ElasticsearchPersistenceActor>()
            .WithRouter(new SmallestMailboxPool(resizeActorConfiguration.LowerBound)
                .WithResizer(new DefaultResizer(resizeActorConfiguration.LowerBound,
                    resizeActorConfiguration.UpperBound)))
            .WithSupervisorStrategy(ParentActor.GetDefaultStrategy()), nameof(ElasticsearchPersistenceActor));
    }

    public static void AddCustomServicesAndConfigurations(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Services
        services.AddElasticSearch(c => configuration
            .GetSection(nameof(ElasticsearchConfig)).Bind(c));
        services.AddScoped<ISearchService, SearchService>();
        services.AddActorSystem(c => configuration.GetSection(nameof(ResizeActorConfig)).Bind(c));
    }
}