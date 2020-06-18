using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Web.Framework.Components;

namespace Nop.Plugin.Payments.Laybuy.Components
{
    /// <summary>
    /// Represents the view component to display payment info in public store
    /// </summary>
    [ViewComponent(Name = LaybuyDefaults.PAYMENT_INFO_VIEW_COMPONENT)]
    public class PaymentInfoViewComponent : NopViewComponent
    {
        #region Fields

        private readonly LaybuyManager _laybuyManager;

        #endregion

        #region Ctor

        public PaymentInfoViewComponent(LaybuyManager laybuyManager)
        {
            _laybuyManager = laybuyManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invoke view component
        /// </summary>
        /// <param name="widgetZone">Widget zone name</param>
        /// <param name="additionalData">Additional data</param>
        /// <returns>View component result</returns>
        public IViewComponentResult Invoke(string widgetZone, object additionalData)
        {
            var (_, initialPrice, price) = _laybuyManager.PreparePriceBreakdown();
            return View("~/Plugins/Payments.Laybuy/Views/PaymentInfo.cshtml", (initialPrice, price));
        }

        #endregion
    }
}