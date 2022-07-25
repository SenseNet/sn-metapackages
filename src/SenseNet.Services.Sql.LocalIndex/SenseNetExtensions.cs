using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Search.Lucene29;
using Task = System.Threading.Tasks.Task;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public static class SenseNetExtensions
    {
        public static IServiceCollection AddSenseNetSqlLocalIndex(this IServiceCollection services, IConfiguration configuration,
            Action<RepositoryBuilder, IServiceProvider> buildRepository = null,
            Func<RepositoryInstance, IServiceProvider, Task> onRepositoryStartedAsync = null)
        {
            // [sensenet]: Set options for EFCSecurityDataProvider
            services.AddOptions<Security.EFCSecurityStore.Configuration.DataOptions>()
                .Configure<IOptions<ConnectionStringOptions>>((securityOptions, systemConnections) =>
                    securityOptions.ConnectionString = systemConnections.Value.Security);


            // add default sensenet services
            services.AddSenseNet(configuration, (repositoryBuilder, provider) =>
            {
                var searchEngineLogger = repositoryBuilder.Services.GetService<ILogger<Lucene29SearchEngine>>();

                // add package-specific repository components
                repositoryBuilder
                    .UseLogger(provider)
                    .UseLucene29LocalSearchEngine(searchEngineLogger, 
                        Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex"));

                buildRepository?.Invoke(repositoryBuilder, provider);
            },
            onRepositoryStartedAsync)
                .AddSenseNetMsSqlProviders(configureInstallation: installOptions =>
                {
                    configuration.Bind("sensenet:install:mssql", installOptions);
                })
                .AddEFCSecurityDataProvider()
                .AddSenseNetOData()
                .AddSenseNetWebHooks()
                .AddSenseNetWopi();

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
