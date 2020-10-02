using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.Security.EFCSecurityStore;
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
                    .UseSecurityDataProvider(
                        new EFCSecurityDataProvider(connectionString: ConnectionStrings.ConnectionString))
                    .UseLucene29LocalSearchEngine(Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex"));

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
