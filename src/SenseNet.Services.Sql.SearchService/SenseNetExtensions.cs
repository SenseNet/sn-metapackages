using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Components;
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
                    .UseLucene29CentralizedSearchEngineWithGrpc(grpcConfig)
                    .UseMsSqlExclusiveLockDataProviderExtension();

                buildRepository?.Invoke(repositoryBuilder, provider);
            },
            onRepositoryStartedAsync)
                .AddComponent(provider => new MsSqlExclusiveLockComponent());

            return services;
        }

        /// <summary>
        /// Registers sensenet middlewares.
        /// If you want to inject a middleware after the user was authenticated, use the onAfterAuthentication method parameter. 
        /// </summary>
        /// <remarks>
        /// Please note that some of the middlewares (e.g. OData) are branching the pipeline. That means you cannot register
        /// a custom middleware that runs after them. To be able to that, you have to call the Use methods in this method
        /// directly and specify an app builder branch there.
        /// </remarks>
        /// <param name="app">The application builder instance.</param>
        /// <param name="onAfterAuthentication">An app builder method that can be used to register middlewares after
        /// authentication but before the main sensenet middlewares (e.g. OData).</param>
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
