using System;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents refund details
    /// </summary>
    public class RefundDetails
    {
        /// <summary>
        /// Gets or sets the refund identifier
        /// </summary>
        [JsonProperty(PropertyName = "refundId")]
        public int? RefundId { get; set; }

        /// <summary>
        /// Gets or sets the date and time of the refund
        /// </summary>
        [JsonProperty(PropertyName = "dateTime")]
        public DateTime? RefundDate { get; set; }

        /// <summary>
        /// Gets or sets the refunded amount
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal? Amount { get; set; }

        /// <summary>
        /// Gets or sets the merchant's refund reference 
        /// </summary>
        [JsonProperty(PropertyName = "refundReference")]
        public string RefundReference { get; set; }

        /// <summary>
        /// Gets or sets the user that processed the refund
        /// </summary>
        [JsonProperty(PropertyName = "user")]
        public string User { get; set; }

        /// <summary>
        /// Gets or sets the user's note (if any) for the refund
        /// </summary>
        [JsonProperty(PropertyName = "userNote")]
        public string UserNote { get; set; }
    }
}