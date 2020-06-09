using Microsoft.AspNetCore.Mvc;
using Nop.Plugin.Payments.Laybuy.Models;
using Nop.Plugin.Payments.Laybuy.Services;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Messages;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.Laybuy.Controllers
{
    [Area(AreaNames.Admin)]
    [HttpsRequirement]
    [AutoValidateAntiforgeryToken]
    [ValidateIpAddress]
    [AuthorizeAdmin]
    public class LaybuyController : BasePaymentController
    {
        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly LaybuyManager _laybuyManager;
        private readonly LaybuySettings _laybuySettings;

        #endregion

        #region Ctor

        public LaybuyController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            LaybuyManager laybuyManager,
            LaybuySettings laybuySettings)
        {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _laybuyManager = laybuyManager;
            _laybuySettings = laybuySettings;
        }

        #endregion

        #region Methods

        public IActionResult Configure()
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            var model = new ConfigurationModel
            {
                MerchantId = _laybuySettings.MerchantId,
                AuthenticationKey = _laybuySettings.AuthenticationKey,
                UseSandbox = _laybuySettings.UseSandbox,
                DisplayPriceBreakdownOnProductPage = _laybuySettings.DisplayPriceBreakdownOnProductPage,
                DisplayPriceBreakdownInProductBox = _laybuySettings.DisplayPriceBreakdownInProductBox,
                DisplayPriceBreakdownInShoppingCart = _laybuySettings.DisplayPriceBreakdownInShoppingCart
            };

            var (currencySupported, currencyCode) = _laybuyManager.PrimaryStoreCurrencySupported();
            if (!currencySupported)
            {
                var url = Url.Action("List", "Currency");
                var warning = string.Format(_localizationService.GetResource("Plugins.Payments.Laybuy.Currency.Warning"), url, currencyCode);
                _notificationService.WarningNotification(warning, false);
            }

            return View("~/Plugins/Payments.Laybuy/Views/Configure.cshtml", model);
        }

        [HttpPost]
        public IActionResult Configure(ConfigurationModel model)
        {
            if (!_permissionService.Authorize(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return Configure();

            _laybuySettings.MerchantId = model.MerchantId;
            _laybuySettings.AuthenticationKey = model.AuthenticationKey;
            _laybuySettings.UseSandbox = model.UseSandbox;
            _laybuySettings.DisplayPriceBreakdownOnProductPage = model.DisplayPriceBreakdownOnProductPage;
            _laybuySettings.DisplayPriceBreakdownInProductBox = model.DisplayPriceBreakdownInProductBox;
            _laybuySettings.DisplayPriceBreakdownInShoppingCart = model.DisplayPriceBreakdownInShoppingCart;
            _settingService.SaveSetting(_laybuySettings);

            _notificationService.SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        #endregion
    }
}