using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Services.Messages;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Laybuy.Controllers
{
    [WwwRequirement]
    [CheckAccessPublicStore]
    [CheckAccessClosedStore]
    [CheckLanguageSeoCode]
    [CheckDiscountCoupon]
    [CheckAffiliate]
    public class LaybuyIpnController : BasePaymentController
    {
        #region Fields

        private readonly INotificationService _notificationService;
        private readonly LaybuyManager _laybuyManager;

        #endregion

        #region Ctor

        public LaybuyIpnController(INotificationService notificationService,
            LaybuyManager laybuyManager)
        {
            _notificationService = notificationService;
            _laybuyManager = laybuyManager;
        }

        #endregion

        #region Methods

        public IActionResult IpnHandler(int orderId)
        {
            var (result, errorMessage) = _laybuyManager.ConfirmOrder(orderId);
            if (!result || !string.IsNullOrEmpty(errorMessage))
            {
                _notificationService.ErrorNotification(errorMessage);
                return RedirectToRoute(LaybuyDefaults.OrderDetailsRouteName, new { orderId });
            }

            return RedirectToRoute(LaybuyDefaults.CheckoutCompletedRouteName, new { orderId });
        }

        #endregion
    }
}