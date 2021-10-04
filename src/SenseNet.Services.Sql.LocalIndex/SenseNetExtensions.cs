using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Components;
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
            // add default sensenet services
            services.AddSenseNet(configuration, (repositoryBuilder, provider) =>
            {
                // add package-specific repository components
                repositoryBuilder
                    .UseLogger(provider)
                    .UseTracer(provider)
                    .UseLucene29LocalSearchEngine(Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex"))
                    .UseMsSqlExclusiveLockDataProviderExtension();

                buildRepository?.Invoke(repositoryBuilder, provider);
            },
            onRepositoryStartedAsync)
                .AddSenseNetMsSqlDataProvider()
                .AddEFCSecurityDataProvider(options =>
                {
                    options.ConnectionString = ConnectionStrings.ConnectionString;
                })
                .AddSenseNetMsSqlStatisticalDataProvider()
                .AddSenseNetMsSqlClientStoreDataProvider()
                .AddComponent(provider => new MsSqlExclusiveLockComponent())
                .AddComponent(provider => new MsSqlStatisticsComponent())
                .AddComponent(provider => new MsSqlClientStoreComponent())
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
