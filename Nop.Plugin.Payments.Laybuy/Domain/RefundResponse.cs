using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents response for a request to refund order
    /// </summary>
    public class RefundResponse : Response
    {
        /// <summary>
        /// Gets or sets the refund identifier
        /// </summary>
        [JsonProperty(PropertyName = "refundId")]
        public int? RefundId { get; set; }

        /// <summary>
        /// Gets or sets the merchant's unique reference for the order
        /// </summary>
        [JsonProperty(PropertyName = "merchantReference")]
        public string MerchantReference { get; set; }
    }
}