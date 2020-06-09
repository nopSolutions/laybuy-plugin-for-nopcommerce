using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents request to refund order
    /// </summary>
    public class RefundRequest : Request
    {
        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        [JsonProperty(PropertyName = "orderId")]
        public int? OrderId { get; set; }

        /// <summary>
        /// Gets or sets the amount to refund on the order, up to the order total
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// Gets or sets the merchant's unique reference for the refund
        /// </summary>
        [JsonProperty(PropertyName = "refundReference")]
        public string RefundReference { get; set; }

        /// <summary>
        /// Gets or sets the brief description of the reason for the refund
        /// </summary>
        [JsonProperty(PropertyName = "note")]
        public string Note { get; set; }

        /// <summary>
        /// Gets the request path
        /// </summary>
        public override string Path => $"order/refund";

        /// <summary>
        /// Gets the request method
        /// </summary>
        public override string Method => HttpMethods.Post;
    }
}