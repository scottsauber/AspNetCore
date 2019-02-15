using System;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Microsoft.AspNetCore.Routing
{
    public static class HubEndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder builder, string pattern) where THub : Hub
        {
            return builder.MapHub<THub>(pattern, configureOptions: null);
        }

        /// <summary>
        /// Maps incoming requests with the specified path to the specified <see cref="Hub"/> type.
        /// </summary>
        /// <typeparam name="THub">The <see cref="Hub"/> type to map requests to.</typeparam>
        /// <param name="builder">The <see cref="IEndpointRouteBuilder"/></param>
        /// <param name="pattern">The request path.</param>
        /// <param name="configureOptions">A callback to configure dispatcher options.</param>
        public static IEndpointConventionBuilder MapHub<THub>(this IEndpointRouteBuilder builder, string pattern, Action<HttpConnectionDispatcherOptions> configureOptions) where THub : Hub
        {
            var marker = builder.ServiceProvider.GetService<SignalRMarkerService>();

            if (marker == null)
            {
                throw new InvalidOperationException("Unable to find the required services. Please add all the required services by calling " +
                                                    "'IServiceCollection.AddSignalR' inside the call to 'ConfigureServices(...)' in the application startup code.");
            }

            var options = new HttpConnectionDispatcherOptions();
            // REVIEW: WE should consider removing this and instead just relying on the
            // AuthorizationMiddleware
            var attributes = typeof(THub).GetCustomAttributes(inherit: true);
            foreach (var attribute in attributes.OfType<AuthorizeAttribute>())
            {
                options.AuthorizationData.Add(attribute);
            }

            configureOptions?.Invoke(options);

            var conventionBuilder = builder.MapConnections(pattern, options, b =>
            {
                b.UseHub<THub>();
            });

            conventionBuilder.Add(e =>
            {
                // Add all attributes on the Hub has metadata (this will allow for things like)
                // auth attributes and cors attributes to work seamlessly
                foreach (var item in attributes)
                {
                    e.Metadata.Add(item);
                }
            });

            return conventionBuilder;
        }
    }
}