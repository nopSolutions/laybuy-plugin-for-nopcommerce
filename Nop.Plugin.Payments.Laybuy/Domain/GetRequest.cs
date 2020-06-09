using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents request to get order
    /// </summary>
    public class GetRequest : Request
    {
        /// <summary>
        /// Gets or sets the merchant's unique reference for the order
        /// </summary>
        [JsonIgnore]
        public string MerchantReference { get; set; }

        /// <summary>
        /// Gets the request path
        /// </summary>
        public override string Path => $"order/merchant/{MerchantReference}";

        /// <summary>
        /// Gets the request method
        /// </summary>
        public override string Method => HttpMethods.Get;
    }
}