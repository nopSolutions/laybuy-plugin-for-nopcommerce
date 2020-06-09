using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents response for a request to confirm order
    /// </summary>
    public class ConfirmResponse : Response
    {
        /// <summary>
        /// Gets or sets the order identifier
        /// </summary>
        [JsonProperty(PropertyName = "orderId")]
        public int? OrderId { get; set; }
    }
}