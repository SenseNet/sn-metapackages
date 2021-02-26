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
                .AddComponent(provider => new MsSqlExclusiveLockComponent())
                .AddSenseNetWebHooks();

            return services;
        }

        /// <summary>
        /// Registers sensenet middlewares.
        /// If you want to inject a middleware before or after one of the built-in middlewares, use the
        /// <see cref="MiddlewareBuilder"/> parameter for defining application builder methods. 
        /// </summary>
        /// <param name="app">The application builder instance.</param>
        /// <param name="middlewareBuilder">Defines optional custom middlewares that will be registered before
        /// or after certain sensenet middlewares (e.g. authentication or OData).</param>
        public static IApplicationBuilder UseSenseNet(this IApplicationBuilder app, MiddlewareBuilder middlewareBuilder = null)
        {
            middlewareBuilder?.OnBeforeCors?.Invoke(app);
            app.UseSenseNetCors();
            middlewareBuilder?.OnAfterCors?.Invoke(app);

            middlewareBuilder?.OnBeforeAuthentication?.Invoke(app);
            app.UseSenseNetAuthentication(); // use Authentication and set User.Current
            middlewareBuilder?.OnAfterAuthentication?.Invoke(app);

            middlewareBuilder?.OnBeforeMembershipExtenders?.Invoke(app);
            app.UseSenseNetMembershipExtenders();
            middlewareBuilder?.OnAfterMembershipExtenders?.Invoke(app);

            // conditional, terminating middlewares

            app.UseSenseNetFiles(middlewareBuilder?.OnBeforeFiles, middlewareBuilder?.OnAfterFiles);

            app.UseSenseNetOdata(middlewareBuilder?.OnBeforeOData, middlewareBuilder?.OnAfterOData);

            app.UseSenseNetWopi(middlewareBuilder?.OnBeforeWopi, middlewareBuilder?.OnAfterWopi);

            return app;
        }
    }
}
