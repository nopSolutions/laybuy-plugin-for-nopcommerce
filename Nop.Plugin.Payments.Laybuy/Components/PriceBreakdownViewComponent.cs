using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Services.Catalog;
using Nop.Services.Directory;
using Nop.Services.Orders;
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

        private readonly ICurrencyService _currencyService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly LaybuyManager _laybuyManager;
        private readonly LaybuySettings _laybuySettings;

        #endregion

        #region Ctor

        public PriceBreakdownViewComponent(ICurrencyService currencyService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPaymentPluginManager paymentPluginManager,
            IPriceFormatter priceFormatter,
            IShoppingCartService shoppingCartService,
            IStoreContext storeContext,
            IWorkContext workContext,
            LaybuyManager laybuyManager,
            LaybuySettings laybuySettings)
        {
            _currencyService = currencyService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _paymentPluginManager = paymentPluginManager;
            _priceFormatter = priceFormatter;
            _shoppingCartService = shoppingCartService;
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

            var (currencySupported, currencyCode) = _laybuyManager.PrimaryStoreCurrencySupported();
            if (!currencySupported)
                return Content(string.Empty);

            var priceValue = decimal.Zero;

            //product details
            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsBottom))
            {
                if (!_laybuySettings.DisplayPriceBreakdownOnProductPage)
                    return Content(string.Empty);

                if (!(additionalData is ProductDetailsModel model))
                    return Content(string.Empty);

                priceValue = model.ProductPrice.PriceValue;
            }

            //product box
            if (widgetZone.Equals(PublicWidgetZones.ProductBoxAddinfoMiddle))
            {
                if (!_laybuySettings.DisplayPriceBreakdownInProductBox)
                    return Content(string.Empty);

                if (!(additionalData is ProductOverviewModel model))
                    return Content(string.Empty);

                priceValue = model.ProductPrice.PriceValue;
            }

            //shopping cart
            if (widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
            {
                if (!_laybuySettings.DisplayPriceBreakdownInShoppingCart)
                    return Content(string.Empty);

                var routeName = HttpContext.GetEndpoint()?.Metadata.GetMetadata<RouteNameMetadata>()?.RouteName;
                if (routeName != LaybuyDefaults.ShoppingCartRouteName)
                    return Content(string.Empty);

                var cart = _shoppingCartService
                    .GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
                var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart);
                priceValue = _currencyService.ConvertFromPrimaryStoreCurrency(cartTotal ?? decimal.Zero, _workContext.WorkingCurrency);
            }

            //whether to use Laybuy Boost price breakdown
            var priceLimit = decimal.MaxValue;
            var firstPrice = decimal.Zero;
            if (currencyCode.Equals("AUD", System.StringComparison.InvariantCultureIgnoreCase) ||
                currencyCode.Equals("NZD", System.StringComparison.InvariantCultureIgnoreCase))
            {
                priceLimit = 1440M;
                firstPrice = 240M;
            }

            if (currencyCode.Equals("GBP", System.StringComparison.InvariantCultureIgnoreCase))
            {
                priceLimit = 720M;
                firstPrice = 120M;
            }

            var initialPrice = string.Empty;
            var priceInPrimaryCurrency = _currencyService.ConvertToPrimaryStoreCurrency(priceValue, _workContext.WorkingCurrency);
            if (priceInPrimaryCurrency > priceLimit)
            {
                var initialPriceValue = _currencyService
                    .ConvertFromPrimaryStoreCurrency(firstPrice + (priceInPrimaryCurrency - priceLimit), _workContext.WorkingCurrency);
                initialPrice = _priceFormatter.FormatPrice(initialPriceValue, true, false);
                priceValue = _currencyService.ConvertFromPrimaryStoreCurrency(firstPrice, _workContext.WorkingCurrency);
            }
            else
                priceValue /= 6;

            var price = _priceFormatter.FormatPrice(priceValue, true, false);
            return View("~/Plugins/Payments.Laybuy/Views/PriceBreakdown/Script.cshtml", (widgetZone, initialPrice, price));
        }

        #endregion
    }
}