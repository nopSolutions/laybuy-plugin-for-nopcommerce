using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents request to cancel order
    /// </summary>
    public class CancelRequest : Request
    {
        /// <summary>
        /// Gets or sets the payment token for the order
        /// </summary>
        [JsonIgnore]
        public string Token { get; set; }

        /// <summary>
        /// Gets the request path
        /// </summary>
        public override string Path => $"order/cancel/{Token}";

        /// <summary>
        /// Gets the request method
        /// </summary>
        public override string Method => HttpMethods.Get;
    }
}