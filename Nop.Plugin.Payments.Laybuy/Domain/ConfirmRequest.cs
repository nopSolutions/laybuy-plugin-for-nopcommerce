using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents request to confirm order
    /// </summary>
    public class ConfirmRequest : Request
    {
        /// <summary>
        /// Gets or sets the payment token for the order
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the total for the customer to pay
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? TotalAmount { get; set; }

        /// <summary>
        /// Gets or sets the currency of the amount
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string Currency { get; set; }

        /// <summary>
        /// Gets or sets items being purchased
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<ItemDetails> Items { get; set; } = new List<ItemDetails>();

        /// <summary>
        /// Gets the request path
        /// </summary>
        public override string Path => $"order/confirm";

        /// <summary>
        /// Gets the request method
        /// </summary>
        public override string Method => HttpMethods.Post;
    }
}