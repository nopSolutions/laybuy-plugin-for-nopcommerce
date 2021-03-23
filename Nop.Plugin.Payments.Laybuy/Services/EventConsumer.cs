using System.Threading.Tasks;
using Nop.Services.Events;
using Nop.Services.Payments;
using Nop.Web.Areas.Admin.Factories;
using Nop.Web.Areas.Admin.Models.Orders;
using Nop.Web.Framework.Events;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.Laybuy.Services
{
    /// <summary>
    /// Represents plugin event consumer
    /// </summary>
    public class EventConsumer : IConsumer<ModelPreparedEvent<BaseNopModel>>
    {
        #region Fields

        private readonly IOrderModelFactory _orderModelFactory;
        private readonly IPaymentPluginManager _paymentPluginManager;
        private readonly LaybuyManager _laybuyManager;

        #endregion

        #region Ctor

        public EventConsumer(IOrderModelFactory orderModelFactory,
            IPaymentPluginManager paymentPluginManager,
            LaybuyManager laybuyManager)
        {
            _orderModelFactory = orderModelFactory;
            _paymentPluginManager = paymentPluginManager;
            _laybuyManager = laybuyManager;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Handle product details model prepared event
        /// </summary>
        /// <param name="eventMessage">Event message</param>
        /// <returns>A task that represents the asynchronous operation</returns>
        public async Task HandleEventAsync(ModelPreparedEvent<BaseNopModel> eventMessage)
        {
            if (eventMessage?.Model is not OrderModel model)
                return;

            if (!await _paymentPluginManager.IsPluginActiveAsync(LaybuyDefaults.SystemName))
                return;

            //clarify refunded amount
            var (order, errorMessage) = await _laybuyManager.CheckRefundsAsync(model.Id);
            if (order != null && string.IsNullOrEmpty(errorMessage))
                await _orderModelFactory.PrepareOrderModelAsync(model, order);
        }

        #endregion
    }
}