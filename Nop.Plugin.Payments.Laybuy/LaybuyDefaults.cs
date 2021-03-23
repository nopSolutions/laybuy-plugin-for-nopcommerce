using Nop.Core;

namespace Nop.Plugin.Payments.Laybuy
{
    /// <summary>
    /// Represents plugin constants
    /// </summary>
    public class LaybuyDefaults
    {
        /// <summary>
        /// Gets the plugin system name
        /// </summary>
        public static string SystemName => "Payments.Laybuy";

        /// <summary>
        /// Gets the user agent used to request third-party services
        /// </summary>
        public static string UserAgent => $"nopCommerce-{NopVersion.CURRENT_VERSION}";

        /// <summary>
        /// Gets the production service URL
        /// </summary>
        public static string ServiceUrl => "https://api.laybuy.com/";

        /// <summary>
        /// Gets the sandbox service URL
        /// </summary>
        public static string SandboxServiceUrl => "https://sandbox-api.laybuy.com/";

        /// <summary>
        /// Gets the configuration route name
        /// </summary>
        public static string ConfigurationRouteName => "Plugin.Payments.Laybuy.Configure";

        /// <summary>
        /// Gets the IPN handler route name
        /// </summary>
        public static string IpnHandlerRouteName => "Plugin.Payments.Laybuy.IPN";

        /// <summary>
        /// Gets the checkout completed route name
        /// </summary>
        public static string CheckoutCompletedRouteName => "CheckoutCompleted";

        /// <summary>
        /// Gets the order details route name
        /// </summary>
        public static string OrderDetailsRouteName => "OrderDetails";

        /// <summary>
        /// Gets the shopping cart route name
        /// </summary>
        public static string ShoppingCartRouteName => "ShoppingCart";

        /// <summary>
        /// Gets the name of a generic attribute to store order token
        /// </summary>
        public static string OrderToken => "LaybuyOrderToken";

        /// <summary>
        /// Gets the name of a generic attribute to store order identifier
        /// </summary>
        public static string OrderId => "LaybuyOrderId";

        /// <summary>
        /// Gets a name of the view component to display price breakdown
        /// </summary>
        public const string PRICE_BREAKDOWN_VIEW_COMPONENT = "LaybuyPriceBreakdownViewComponent";
    }
}