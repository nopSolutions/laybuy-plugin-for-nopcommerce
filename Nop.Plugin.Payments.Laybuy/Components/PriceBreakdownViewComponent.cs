using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Services.Payments;
using Nop.Web.Framework.Components;
using Nop.Web.Framework.Infrastructure;
using Nop.Web.Models.Catalog;

namespace Nop.Plugin.Payments.Laybuy.Components
{
    /// <summary>
    /// Represents the view component to display price breakdown
    /// </summary>
    [ViewComponent(Name = LaybuyDefaults.PRICE_BREAKDOWN_VIEW_COMPONENT)]
    public class PriceBreakdownViewComponent : NopViewComponent
    {
        #region Fields

        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly LaybuyManager _laybuyManager;
        private readonly LaybuySettings _laybuySettings;

        #endregion

        #region Ctor

        public PriceBreakdownViewComponent(IPaymentPluginManager paymentPluginManager,
            IStoreContext storeContext,
            IWorkContext workContext,
            LaybuyManager laybuyManager,
            LaybuySettings laybuySettings)
        {
            _paymentPluginManager = paymentPluginManager;
            _storeContext = storeContext;
            _workContext = workContext;
            _laybuyManager = laybuyManager;
            _laybuySettings = laybuySettings;
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
            if (!_paymentPluginManager.IsPluginActive(LaybuyDefaults.SystemName, _workContext.CurrentCustomer, _storeContext.CurrentStore.Id))
                return Content(string.Empty);

            var result = false;
            var initialPrice = string.Empty;
            var price = string.Empty;

            //product details
            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsBottom))
            {
                if (!_laybuySettings.DisplayPriceBreakdownOnProductPage)
                    return Content(string.Empty);

                if (!(additionalData is ProductDetailsModel model))
                    return Content(string.Empty);

                (result, initialPrice, price) = _laybuyManager.PreparePriceBreakdown(model.ProductPrice.PriceValue);
            }

            //product box
            if (widgetZone.Equals(PublicWidgetZones.ProductBoxAddinfoMiddle))
            {
                if (!_laybuySettings.DisplayPriceBreakdownInProductBox)
                    return Content(string.Empty);

                if (!(additionalData is ProductOverviewModel model))
                    return Content(string.Empty);

                (result, initialPrice, price) = _laybuyManager.PreparePriceBreakdown(model.ProductPrice.PriceValue);
            }

            //shopping cart
            if (widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
            {
                if (!_laybuySettings.DisplayPriceBreakdownInShoppingCart)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != LaybuyDefaults.ShoppingCartRouteName)
                    return Content(string.Empty);

                (result, initialPrice, price) = _laybuyManager.PreparePriceBreakdown();
            }

            //whether to display price breakdown
            if (!result)
                return Content(string.Empty);

            return View("~/Plugins/Payments.Laybuy/Views/PriceBreakdown/Script.cshtml", (widgetZone, initialPrice, price));
        }

        #endregion
    }
}