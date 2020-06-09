using FluentValidation;
using Nop.Plugin.Payments.Laybuy.Models;
using Nop.Services.Localization;
using Nop.Web.Framework.Validators;

namespace Nop.Plugin.Payments.Laybuy.Validators
{
    /// <summary>
    /// Represents configuration model validator
    /// </summary>
    public class ConfigurationValidator : BaseNopValidator<ConfigurationModel>
    {
        #region Ctor

        public ConfigurationValidator(ILocalizationService localizationService)
        {
            RuleFor(model => model.MerchantId)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.Laybuy.Fields.MerchantId.Required"))
                .When(model => !model.UseSandbox);

            RuleFor(model => model.AuthenticationKey)
                .NotEmpty()
                .WithMessage(localizationService.GetResource("Plugins.Payments.Laybuy.Fields.AuthenticationKey.Required"))
                .When(model => !model.UseSandbox);
        }

        #endregion
    }
}