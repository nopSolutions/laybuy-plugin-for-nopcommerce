using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents request to create order
    /// </summary>
    public class CreateRequest : Request
    {
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
        /// Gets or sets the relative/absolute URL to redirect the customer to once the payment process is completed
        /// </summary>
        [JsonProperty(PropertyName = "returnUrl")]
        public string ReturnUrl { get; set; }

        /// <summary>
        /// Gets or sets the merchant's unique reference for the order
        /// </summary>
        [JsonProperty(PropertyName = "merchantReference")]
        public string MerchantReference { get; set; }

        /// <summary>
        /// Gets or sets the amount of tax contained within amount
        /// </summary>
        [JsonProperty(PropertyName = "tax")]
        public decimal? TaxAmount { get; set; }

        /// <summary>
        /// Gets or sets the customer details
        /// </summary>
        [JsonProperty(PropertyName = "customer")]
        public CustomerDetails Customer { get; set; }

        /// <summary>
        /// Gets or sets the billing address details
        /// </summary>
        [JsonProperty(PropertyName = "billingAddress")]
        public AddressDetails BillingAddress { get; set; }

        /// <summary>
        /// Gets or sets the shipping address details
        /// </summary>
        [JsonProperty(PropertyName = "shippingAddress")]
        public AddressDetails ShippingAddress { get; set; }

        /// <summary>
        /// Gets or sets items being purchased
        /// </summary>
        [JsonProperty(PropertyName = "items")]
        public List<ItemDetails> Items { get; set; } = new List<ItemDetails>();

        /// <summary>
        /// Gets the request path
        /// </summary>
        public override string Path => $"order/create";

        /// <summary>
        /// Gets the request method
        /// </summary>
        public override string Method => HttpMethods.Post;
    }
}