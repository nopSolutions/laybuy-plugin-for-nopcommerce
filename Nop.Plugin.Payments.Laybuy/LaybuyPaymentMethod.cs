using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Nop.Core;
using Nop.Core.Domain.Cms;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Services.Cms;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Payments;
using Nop.Services.Plugins;
using Nop.Web.Framework.Infrastructure;

namespace Nop.Plugin.Payments.Laybuy
{
    /// <summary>
    /// Represents Laybuy payment method
    /// </summary>
    public class LaybuyPaymentMethod : BasePlugin, IPaymentMethod, IWidgetPlugin
    {
        #region Fields

        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IWebHelper _webHelper;
        private readonly LaybuyManager _laybuyManager;
        private readonly WidgetSettings _widgetSettings;

        #endregion

        #region Ctor

        public LaybuyPaymentMethod(IActionContextAccessor actionContextAccessor,
            IGenericAttributeService genericAttributeService,
            ILocalizationService localizationService,
            INotificationService notificationService,
            ISettingService settingService,
            IUrlHelperFactory urlHelperFactory,
            IWebHelper webHelper,
            LaybuyManager laybuyManager,
            WidgetSettings widgetSettings)
        {
            _actionContextAccessor = actionContextAccessor;
            _genericAttributeService = genericAttributeService;
            _localizationService = localizationService;
            _notificationService = notificationService;
            _settingService = settingService;
            _urlHelperFactory = urlHelperFactory;
            _webHelper = webHelper;
            _laybuyManager = laybuyManager;
            _widgetSettings = widgetSettings;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult());
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest == null)
                throw new ArgumentNullException(nameof(postProcessPaymentRequest));

            //prepare URLs
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
            var successUrl = urlHelper.RouteUrl(LaybuyDefaults.IpnHandlerRouteName,
                new { orderId = postProcessPaymentRequest.Order.Id }, _webHelper.GetCurrentRequestProtocol());
            var failUrl = urlHelper.RouteUrl(LaybuyDefaults.OrderDetailsRouteName,
                new { orderId = postProcessPaymentRequest.Order.Id }, _webHelper.GetCurrentRequestProtocol());

            //try to create order
            var (response, errorMessage) = await _laybuyManager.CreateOrderAsync(postProcessPaymentRequest.Order, successUrl);
            if (!string.IsNullOrEmpty(response?.PaymentUrl) && string.IsNullOrEmpty(errorMessage))
            {
                //redirect to payment link on success
                await _genericAttributeService.SaveAttributeAsync(postProcessPaymentRequest.Order, LaybuyDefaults.OrderToken, response.Token);
                _actionContextAccessor.ActionContext.HttpContext.Response.Redirect(response.PaymentUrl);
                return;
            }

            //redirect to order details on fail
            _notificationService.ErrorNotification(errorMessage);
            _actionContextAccessor.ActionContext.HttpContext.Response.Redirect(failUrl);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the capture payment result
        /// </returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult { Errors = new[] { "Capture method not supported" } });
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public async Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            //try to refund order
            var (_, errorMessage) = await _laybuyManager.RefundOrderAsync(refundPaymentRequest.Order, refundPaymentRequest.AmountToRefund);
            if (!string.IsNullOrEmpty(errorMessage))
                return new RefundPaymentResult { Errors = new[] { errorMessage } };

            //request succeeded
            return new RefundPaymentResult
            {
                NewPaymentStatus = refundPaymentRequest.IsPartialRefund ? PaymentStatus.PartiallyRefunded : PaymentStatus.Refunded
            };
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult { Errors = new[] { "Void method not supported" } });
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the process payment result
        /// </returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            return Task.FromResult(new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return Task.FromResult(new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } });
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the rue - hide; false - display.
        /// </returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the additional handling fee
        /// </returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(decimal.Zero);
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the result
        /// </returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            return Task.FromResult(true);
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the list of validating errors
        /// </returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            return Task.FromResult<IList<string>>(new List<string>());
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the payment info holder
        /// </returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            return Task.FromResult(new ProcessPaymentRequest());
        }

        /// <summary>
        /// Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext).RouteUrl(LaybuyDefaults.ConfigurationRouteName);
        }

        /// <summary>
        /// Gets a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <param name="viewComponentName">View component name</param>
        public string GetPublicViewComponentName()
        {
            return null;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>
        /// A task that represents the asynchronous operation
        /// The task result contains the widget zones
        /// </returns>
        public Task<IList<string>> GetWidgetZonesAsync()
        {
            return Task.FromResult<IList<string>>(new List<string>
            {
                PublicWidgetZones.ProductDetailsBottom,
                PublicWidgetZones.ProductBoxAddinfoMiddle,
                PublicWidgetZones.OrderSummaryContentAfter,
            });
        }

        /// <summary>
        /// Gets a name of a view component for displaying widget
        /// </summary>
        /// <param name="widgetZone">Name of the widget zone</param>
        /// <returns>View component name</returns>
        public string GetWidgetViewComponentName(string widgetZone)
        {
            if (widgetZone == null)
                throw new ArgumentNullException(nameof(widgetZone));

            if (widgetZone.Equals(PublicWidgetZones.ProductDetailsBottom) ||
                widgetZone.Equals(PublicWidgetZones.ProductBoxAddinfoMiddle) ||
                widgetZone.Equals(PublicWidgetZones.OrderSummaryContentAfter))
            {
                return LaybuyDefaults.PRICE_BREAKDOWN_VIEW_COMPONENT;
            }

            return string.Empty;
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task InstallAsync()
        {
            await _settingService.SaveSettingAsync(new LaybuySettings
            {
                UseSandbox = true,
                DisplayPriceBreakdownOnProductPage = true,
                DisplayPriceBreakdownInProductBox = true,
                DisplayPriceBreakdownInShoppingCart = true,
                RequestTimeout = 10
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(LaybuyDefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(LaybuyDefaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.Laybuy.Currency.Warning"] = "The <a href=\"{0}\" target=\"_blank\">primary store currency</a> ({1}) isn't supported by Laybuy. New Zealand Dollars (NZD), Australian Dollars (AUD) and British Pound (GBP) are currently the only currencies supported.",
                ["Plugins.Payments.Laybuy.Fields.MerchantId"] = "Merchant ID",
                ["Plugins.Payments.Laybuy.Fields.MerchantId.Hint"] = "Enter your Laybuy Merchant ID.",
                ["Plugins.Payments.Laybuy.Fields.MerchantId.Required"] = "Merchant ID is required",
                ["Plugins.Payments.Laybuy.Fields.AuthenticationKey"] = "Authentication Key",
                ["Plugins.Payments.Laybuy.Fields.AuthenticationKey.Hint"] = "Enter your Laybuy Authentication Key.",
                ["Plugins.Payments.Laybuy.Fields.AuthenticationKey.Required"] = "Authentication Key is required",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInProductBox"] = "Display in product box",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInProductBox.Hint"] = "Check to display price breakdown in a product box (e.g. on a category page).",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInShoppingCart"] = "Display in shopping cart",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownInShoppingCart.Hint"] = "Check to display price breakdown in the shopping cart.",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownOnProductPage"] = "Display on product page",
                ["Plugins.Payments.Laybuy.Fields.DisplayPriceBreakdownOnProductPage.Hint"] = "Check to display price breakdown on a product page.",
                ["Plugins.Payments.Laybuy.Fields.UseSandbox"] = "Use sandbox",
                ["Plugins.Payments.Laybuy.Fields.UseSandbox.Hint"] = "Determine whether to use the sandbox environment for testing purposes.",
                ["Plugins.Payments.Laybuy.PaymentMethodDescription"] = "You will be redirected to Laybuy to complete the payment",
                ["Plugins.Payments.Laybuy.PriceBreakdown"] = "Laybuy price breakdown"
            });

            await base.InstallAsync();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public override async Task UninstallAsync()
        {
            if (_widgetSettings.ActiveWidgetSystemNames.Contains(LaybuyDefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(LaybuyDefaults.SystemName);
                await _settingService.SaveSettingAsync(_widgetSettings);
            }
            await _settingService.DeleteSettingAsync<LaybuySettings>();
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.Laybuy");
            await base.UninstallAsync();
        }

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.Laybuy.PaymentMethodDescription");
        }

        #endregion

        #region Properies

        /// <summary>
        /// Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        /// Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => true;

        /// <summary>
        /// Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => true;

        /// <summary>
        /// Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        /// <summary>
        /// Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        /// Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => true;

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion
    }
}