using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents response for a request to get order
    /// </summary>
    public class GetResponse : Response
    {
        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        [JsonProperty(PropertyName = "orderId")]
        public int? OrderId { get; set; }

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
        /// Gets or sets the merchant's unique reference for the order
        /// </summary>
        [JsonProperty(PropertyName = "merchantReference")]
        public string MerchantReference { get; set; }

        /// <summary>
        /// Gets or sets the date/time that the order was processed
        /// </summary>
        [JsonProperty(PropertyName = "processed")]
        public DateTime? ProcessedDate { get; set; }

        /// <summary>
        /// Gets or sets the customer details
        /// </summary>
        [JsonProperty(PropertyName = "customer")]
        public CustomerDetails Customer { get; set; }

        /// <summary>
        /// Gets or sets refunds for the order
        /// </summary>
        [JsonProperty(PropertyName = "refunds")]
        public List<RefundDetails> Refunds { get; set; }
    }
}