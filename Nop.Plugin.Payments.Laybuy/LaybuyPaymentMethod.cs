using System;
using System.Collections.Generic;
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
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult();
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            if (postProcessPaymentRequest == null)
                throw new ArgumentNullException(nameof(postProcessPaymentRequest));

            //prepare URLs
            var urlHelper = _urlHelperFactory.GetUrlHelper(_actionContextAccessor.ActionContext);
            var successUrl = urlHelper.RouteUrl(LaybuyDefaults.IpnHandlerRouteName,
                new { orderId = postProcessPaymentRequest.Order.Id }, _webHelper.CurrentRequestProtocol);
            var failUrl = urlHelper.RouteUrl(LaybuyDefaults.OrderDetailsRouteName,
                new { orderId = postProcessPaymentRequest.Order.Id }, _webHelper.CurrentRequestProtocol);

            //try to create order
            var (response, errorMessage) = _laybuyManager.CreateOrder(postProcessPaymentRequest.Order, successUrl);
            if (!string.IsNullOrEmpty(response?.PaymentUrl) && string.IsNullOrEmpty(errorMessage))
            {
                //redirect to payment link on success
                _genericAttributeService.SaveAttribute(postProcessPaymentRequest.Order, LaybuyDefaults.OrderToken, response.Token);
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
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            return new CapturePaymentResult { Errors = new[] { "Capture method not supported" } };
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            //try to refund order
            var (_, errorMessage) = _laybuyManager.RefundOrder(refundPaymentRequest.Order, refundPaymentRequest.AmountToRefund);
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
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            return new VoidPaymentResult { Errors = new[] { "Void method not supported" } };
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            return new ProcessPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            return new CancelRecurringPaymentResult { Errors = new[] { "Recurring payment not supported" } };
        }

        /// <summary>
        /// Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        /// <summary>
        /// Gets additional handling fee
        /// </summary>
        /// <param name="cart">Shoping cart</param>
        /// <returns>Additional handling fee</returns>
        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return decimal.Zero;
        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public bool CanRePostProcessPayment(Order order)
        {
            return true;
        }

        /// <summary>
        /// Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public IList<string> ValidatePaymentForm(IFormCollection form)
        {
            return new List<string>();
        }

        /// <summary>
        /// Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public ProcessPaymentRequest GetPaymentInfo(IFormCollection form)
        {
            return new ProcessPaymentRequest();
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
            return LaybuyDefaults.PAYMENT_INFO_VIEW_COMPONENT;
        }

        /// <summary>
        /// Gets widget zones where this widget should be rendered
        /// </summary>
        /// <returns>Widget zones</returns>
        public IList<string> GetWidgetZones()
        {
            return new List<string>
            {
                PublicWidgetZones.ProductDetailsBottom,
                PublicWidgetZones.ProductBoxAddinfoMiddle,
                PublicWidgetZones.OrderSummaryContentAfter,
            };
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
        public override void Install()
        {
            //settings
            _settingService.SaveSetting(new LaybuySettings
            {
                UseSandbox = true
            });

            if (!_widgetSettings.ActiveWidgetSystemNames.Contains(LaybuyDefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Add(LaybuyDefaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }

            //locales
            _localizationService.AddPluginLocaleResource(new Dictionary<string, string>
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

            base.Install();
        }

        /// <summary>
        /// Uninstall the plugin
        /// </summary>
        public override void Uninstall()
        {
            //settings
            if (_widgetSettings.ActiveWidgetSystemNames.Contains(LaybuyDefaults.SystemName))
            {
                _widgetSettings.ActiveWidgetSystemNames.Remove(LaybuyDefaults.SystemName);
                _settingService.SaveSetting(_widgetSettings);
            }
            _settingService.DeleteSetting<LaybuySettings>();

            //locales
            _localizationService.DeletePluginLocaleResources("Plugins.Payments.Laybuy");

            base.Uninstall();
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
        public bool SkipPaymentInfo => false;

        /// <summary>
        /// Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        public string PaymentMethodDescription => _localizationService.GetResource("Plugins.Payments.Laybuy.PaymentMethodDescription");

        /// <summary>
        /// Gets a value indicating whether to hide this plugin on the widget list page in the admin area
        /// </summary>
        public bool HideInWidgetList => true;

        #endregion
    }
}