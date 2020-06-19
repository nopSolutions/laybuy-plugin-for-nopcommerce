using System.ComponentModel.DataAnnotations;
using Nop.Web.Framework.Models;
using Nop.Web.Framework.Mvc.ModelBinding;

namespace Nop.Plugin.Payments.Laybuy.Models
{
    /// <summary>
    /// Represents configuration model
    /// </summary>
    public class ConfigurationModel : BaseNopModel
    {
        #region Properties

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.MerchantId")]
        public string MerchantId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.AuthenticationKey")]
        [DataType(DataType.Password)]
        [NoTrim]
        public string AuthenticationKey { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.UseSandbox")]
        public bool UseSandbox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownOnProductPage")]
        public bool DisplayPriceBreakdownOnProductPage { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInProductBox")]
        public bool DisplayPriceBreakdownInProductBox { get; set; }

        [NopResourceDisplayName("Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInShoppingCart")]
        public bool DisplayPriceBreakdownInShoppingCart { get; set; }

        #endregion
    }
}