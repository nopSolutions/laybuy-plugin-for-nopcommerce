using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.Laybuy
{
    /// <summary>
    /// Represents plugin settings
    /// </summary>
    public class LaybuySettings : ISettings
    {
        /// <summary>
        /// Gets or sets Merchant ID
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        /// Gets or sets Authentication Key
        /// </summary>
        public string AuthenticationKey { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use sandbox environment
        /// </summary>
        public bool UseSandbox { get; set; }

        #region Advanced settings

        /// <summary>
        /// Gets or sets a value indicating whether to display price breakdown on a product page
        /// </summary>
        public bool DisplayPriceBreakdownOnProductPage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display price breakdown in a product box (e.g. on a category page)
        /// </summary>
        public bool DisplayPriceBreakdownInProductBox { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to display price breakdown in the shopping cart
        /// </summary>
        public bool DisplayPriceBreakdownInShoppingCart { get; set; }

        /// <summary>
        /// Gets or sets a period (in seconds) before the request times out
        /// </summary>
        public int? RequestTimeout { get; set; }

        #endregion
    }
}