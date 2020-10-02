using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Search.Lucene29.Centralized.GrpcClient;
using SenseNet.Security.EFCSecurityStore;
using SenseNet.Security.Messaging.RabbitMQ;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class SenseNetExtensions
    {
        public static IServiceCollection AddSenseNetSqlSearchService(this IServiceCollection services, IConfiguration configuration,
            Action<GrpcClientOptions> configureGrpcClient = null,
            Action<RepositoryBuilder, IServiceProvider> buildRepository = null,
            Func<RepositoryInstance, IServiceProvider, Task> onRepositoryStartedAsync = null)
        {
            // configure Grpc options before adding services
            services.Configure<GrpcClientOptions>(configuration.GetSection("sensenet:search:GrpcClient"));
            if (configureGrpcClient != null)
                services.Configure(configureGrpcClient);

            // add default sensenet services
            services.AddSenseNet(configuration, (repositoryBuilder, provider) =>
            {
                var grpcConfig = provider.GetService<IOptions<GrpcClientOptions>>().Value;

                // add package-specific repository components
                repositoryBuilder
                    .UseLogger(provider)
                    .UseTracer(provider)
                    .UseSecurityDataProvider(new EFCSecurityDataProvider(connectionString: ConnectionStrings.ConnectionString))
                    .UseSecurityMessageProvider(new RabbitMQMessageProvider())
                    .UseLucene29CentralizedSearchEngineWithGrpc(grpcConfig);

                buildRepository?.Invoke(repositoryBuilder, provider);
            },
            onRepositoryStartedAsync);

            return services;
        }

        public static IApplicationBuilder UseSenseNet(this IApplicationBuilder app, Action<IApplicationBuilder> onAfterAuthentication = null)
        {
            // custom CORS policy
            app.UseSenseNetCors();

            // use Authentication and set User.Current
            app.UseSenseNetAuthentication();

            onAfterAuthentication?.Invoke(app);

            app.UseSenseNetMembershipExtenders();
            app.UseSenseNetFiles();
            app.UseSenseNetOdata();
            app.UseSenseNetWopi();

            return app;
        }
    }
}
