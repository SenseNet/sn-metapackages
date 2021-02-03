using Microsoft.AspNetCore.Builder;
using System;

// ReSharper disable once CheckNamespace
namespace SenseNet.Extensions.DependencyInjection
{
    public class MiddlewareBuilder
    {
        /// <summary>
        /// Register middlewares before the Cors middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeCors { get; set; }
        /// <summary>
        /// Register middlewares after the Cors middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterCors { get; set; }

        /// <summary>
        /// Register middlewares before the authentication middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeAuthentication { get; set; }
        /// <summary>
        /// Register middlewares after the authentication middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterAuthentication { get; set; }

        /// <summary>
        /// Register middlewares before the MembershipExtender middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeMembershipExtenders { get; set; }
        /// <summary>
        /// Register middlewares after the MembershipExtender middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterMembershipExtenders { get; set; }

        /// <summary>
        /// Register middlewares before the Files middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeFiles { get; set; }
        /// <summary>
        /// Register middlewares after the Files middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterFiles { get; set; }

        /// <summary>
        /// Register middlewares before the OData middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeOData { get; set; }
        /// <summary>
        /// Register middlewares after the OData middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterOData { get; set; }

        /// <summary>
        /// Register middlewares before the Wopi middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnBeforeWopi { get; set; }
        /// <summary>
        /// Register middlewares after the Wopi middleware.
        /// </summary>
        public Action<IApplicationBuilder> OnAfterWopi { get; set; }
    }
}
