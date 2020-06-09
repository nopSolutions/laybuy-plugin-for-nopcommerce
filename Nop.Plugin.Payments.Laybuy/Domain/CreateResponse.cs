using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents response for a request to create order
    /// </summary>
    public class CreateResponse : Response
    {
        /// <summary>
        /// Gets or sets the payment token for the order
        /// </summary>
        [JsonProperty(PropertyName = "token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the payment URL in order to initiate the payment interface
        /// </summary>
        [JsonProperty(PropertyName = "paymentUrl")]
        public string PaymentUrl { get; set; }
    }
}