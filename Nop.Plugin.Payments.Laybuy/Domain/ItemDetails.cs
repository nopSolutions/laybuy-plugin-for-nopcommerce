using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents item details
    /// </summary>
    public class ItemDetails
    {
        /// <summary>
        /// Gets or sets the merchant's unique identifier (id, PLU/SKU, barcode, etc.) for the product
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string ItemId { get; set; }

        /// <summary>
        /// Gets or sets the description of the product
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the quantity being purchased
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public int? Quantity { get; set; }

        /// <summary>
        /// Gets or sets the unit price of the product
        /// </summary>
        [JsonProperty(PropertyName = "price")]
        public decimal? Price { get; set; }
    }
}