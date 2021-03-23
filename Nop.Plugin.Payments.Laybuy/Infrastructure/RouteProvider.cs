using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.Laybuy.Infrastructure
{
    /// <summary>
    /// Represents plugin route provider
    /// </summary>
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        /// Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            endpointRouteBuilder.MapControllerRoute(LaybuyDefaults.ConfigurationRouteName,
                "Plugins/Laybuy/Configure",
                new { controller = "Laybuy", action = "Configure", area = AreaNames.Admin });

            endpointRouteBuilder.MapControllerRoute(LaybuyDefaults.IpnHandlerRouteName,
                "Plugins/Laybuy/IPN/{orderId:min(0)}",
                new { controller = "LaybuyIpn", action = "IpnHandler" });
        }

        /// <summary>
        /// Gets a priority of route provider
        /// </summary>
        public int Priority => 0;
    }
}