using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SenseNet.Configuration;
using SenseNet.ContentRepository;
using SenseNet.ContentRepository.Components;
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
                    .UseLucene29LocalSearchEngine(Path.Combine(Environment.CurrentDirectory, "App_Data", "LocalIndex"))
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
        /// <param name="middlewareBuilder">Defines optional custom middlewares that will be registered before
        /// or after certain sensenet middlewares (e.g. authentication or OData).</param>
        public static IApplicationBuilder UseSenseNet(this IApplicationBuilder app, MiddlewareBuilder middlewareBuilder = null)
        {
            middlewareBuilder?.OnBeforeCors?.Invoke(app);

            // custom CORS policy
            app.UseSenseNetCors();

            middlewareBuilder?.OnAfterCors?.Invoke(app);
            middlewareBuilder?.OnBeforeAuthentication?.Invoke(app);

            // use Authentication and set User.Current
            app.UseSenseNetAuthentication();

            middlewareBuilder?.OnAfterAuthentication?.Invoke(app);
            middlewareBuilder?.OnBeforeMembershipExtenders?.Invoke(app);

            app.UseSenseNetMembershipExtenders();

            middlewareBuilder?.OnAfterMembershipExtenders?.Invoke(app);

            //UNDONE: add after methods when the api is ready
            app.UseSenseNetFiles(middlewareBuilder?.OnBeforeFiles);
            app.UseSenseNetOdata(middlewareBuilder?.OnBeforeOData);
            app.UseSenseNetWopi(middlewareBuilder?.OnBeforeWopi);

            return app;
        }
    }

    public class MiddlewareBuilder
    {
        public Action<IApplicationBuilder> OnBeforeCors { get; set; }
        public Action<IApplicationBuilder> OnAfterCors { get; set; }

        public Action<IApplicationBuilder> OnBeforeAuthentication { get; set; }
        public Action<IApplicationBuilder> OnAfterAuthentication { get; set; }

        public Action<IApplicationBuilder> OnBeforeMembershipExtenders { get; set; }
        public Action<IApplicationBuilder> OnAfterMembershipExtenders { get; set; }

        public Action<IApplicationBuilder> OnBeforeFiles { get; set; }
        public Action<IApplicationBuilder> OnAfterFiles { get; set; }

        public Action<IApplicationBuilder> OnBeforeOData { get; set; }
        public Action<IApplicationBuilder> OnAfterOData { get; set; }

        public Action<IApplicationBuilder> OnBeforeWopi { get; set; }
        public Action<IApplicationBuilder> OnAfterWopi { get; set; }
    }
}
