using System;
using System.Collections.Generic;
using System.Linq;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Plugin.Payments.Laybuy.Domain;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Tax;

namespace Nop.Plugin.Payments.Laybuy.Services
{
    /// <summary>
    /// Represents the service manager
    /// </summary>
    public class LaybuyManager
    {
        #region Fields

        private readonly CurrencySettings _currencySettings;
        private readonly IAddressService _addressService;
        private readonly ICheckoutAttributeParser _checkoutAttributeParser;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly ILogger _logger;
        private readonly IOrderProcessingService _orderProcessingService;
        private readonly IOrderService _orderService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;
        private readonly ITaxService _taxService;
        private readonly IWebHelper _webHelper;
        private readonly IWorkContext _workContext;
        private readonly LaybuyHttpClient _httpClient;
        private readonly LaybuySettings _laybuySettings;

        #endregion

        #region Ctor

        public LaybuyManager(CurrencySettings currencySettings,
            IAddressService addressService,
            ICheckoutAttributeParser checkoutAttributeParser,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IGenericAttributeService genericAttributeService,
            ILogger logger,
            IOrderProcessingService orderProcessingService,
            IOrderService orderService,
            IOrderTotalCalculationService orderTotalCalculationService,
            IPriceFormatter priceFormatter,
            IProductService productService,
            IShoppingCartService shoppingCartService,
            IStateProvinceService stateProvinceService,
            IStoreContext storeContext,
            ITaxService taxService,
            IWebHelper webHelper,
            IWorkContext workContext,
            LaybuyHttpClient httpClient,
            LaybuySettings laybuySettings)
        {
            _currencySettings = currencySettings;
            _addressService = addressService;
            _checkoutAttributeParser = checkoutAttributeParser;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _genericAttributeService = genericAttributeService;
            _logger = logger;
            _orderProcessingService = orderProcessingService;
            _orderService = orderService;
            _orderTotalCalculationService = orderTotalCalculationService;
            _priceFormatter = priceFormatter;
            _productService = productService;
            _shoppingCartService = shoppingCartService;
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _taxService = taxService;
            _webHelper = webHelper;
            _workContext = workContext;
            _httpClient = httpClient;
            _laybuySettings = laybuySettings;
        }

        #endregion

        #region Utilities

        /// <summary>
        /// Check whether the plugin is configured
        /// </summary>
        /// <returns>Result</returns>
        private bool IsConfigured()
        {
            //Merchant ID and Authentication Key are required to request services
            return !string.IsNullOrEmpty(_laybuySettings.MerchantId) && !string.IsNullOrEmpty(_laybuySettings.AuthenticationKey);
        }

        /// <summary>
        /// Handle function and get result
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="function">Function</param>
        /// <returns>Result; error message if exists</returns>
        private (TResult Result, string ErrorMessage) HandleFunction<TResult>(Func<TResult> function)
        {
            try
            {
                //ensure that plugin is configured
                if (!IsConfigured())
                    throw new NopException("Plugin not configured");

                //invoke function
                return (function(), default);
            }
            catch (Exception exception)
            {
                //log errors
                var errorMessage = $"{LaybuyDefaults.SystemName} error: {Environment.NewLine}{exception.Message}";
                _logger.Error(errorMessage, exception, _workContext.CurrentCustomer);

                return (default, errorMessage);
            }
        }

        /// <summary>
        /// Handle request and get response
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request</param>
        /// <returns>Response</returns>
        private TResponse HandleRequest<TRequest, TResponse>(TRequest request) where TRequest : Request where TResponse : Response
        {
            //execute request
            var response = _httpClient.RequestAsync<TRequest, TResponse>(request)?.Result
                ?? throw new NopException("No response from service");

            //ensure that request was successfull
            if (response.Result != ResponseResult.Success)
                throw new NopException($"Request result - {response.Result}. {Environment.NewLine}{response.ErrorMessage}");

            return response;
        }

        /// <summary>
        /// Prepare order items
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Item details</returns>
        private List<ItemDetails> PrepareOrderItems(Order order)
        {
            var items = new List<ItemDetails>();

            //add purchased items
            items.AddRange(_orderService.GetOrderItems(order.Id).Select(item =>
            {
                var product = _productService.GetProductById(item.ProductId);
                var sku = product != null ? _productService.FormatSku(product, item.AttributesXml) : null;
                return new ItemDetails
                {
                    ItemId = !string.IsNullOrEmpty(sku) ? sku : product?.Id.ToString(),
                    Description = product?.Name,
                    Price = item.UnitPriceExclTax,
                    Quantity = item.Quantity
                };
            }));

            //add checkout attributes as order items
            var customer = _customerService.GetCustomerById(order.CustomerId);
            var checkoutAttributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(order.CheckoutAttributesXml);
            foreach (var (attribute, values) in checkoutAttributeValues)
            {
                foreach (var attributeValue in values)
                {
                    var price = _taxService.GetCheckoutAttributePrice(attribute, attributeValue, false, customer);
                    if (price <= decimal.Zero)
                        continue;

                    items.Add(new ItemDetails
                    {
                        ItemId = attribute.Name,
                        Description = $"{attribute.Name} - {attributeValue.Name}",
                        Price = price,
                        Quantity = 1
                    });
                }
            }

            //add shipping cost as a separate order item
            if (order.OrderShippingExclTax > decimal.Zero)
            {
                items.Add(new ItemDetails
                {
                    ItemId = "Shipping",
                    Description = $"Shipping by {order.ShippingRateComputationMethodSystemName}",
                    Price = order.OrderShippingExclTax,
                    Quantity = 1
                });
            }

            //add tax as a separate order item
            if (order.OrderTax > decimal.Zero)
            {
                items.Add(new ItemDetails
                {
                    ItemId = "Tax",
                    Description = $"Order tax amount",
                    Price = order.OrderTax,
                    Quantity = 1
                });
            }

            var itemsTotal = items.Sum(item => item.Price * item.Quantity);
            if (itemsTotal > order.OrderTotal)
            {
                //gift card, rewarded point amount, discount applied to cart considered as a separate order item
                items.Add(new ItemDetails
                {
                    ItemId = "Discount",
                    Description = $"Discounts, gift cards, rewarded point amount applied to cart, etc",
                    Price = order.OrderTotal - itemsTotal,
                    Quantity = 1
                });
            }

            return items;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Check whether the primary store currency is supported by payment gateway
        /// </summary>
        /// <returns>Result; Primary store currency code</returns>
        public (bool Result, string CurrencyCode) PrimaryStoreCurrencySupported()
        {
            //New Zealand Dollars (NZD), Australian Dollars (AUD) and British Pound (GBP) are currently the only currencies supported
            var supportedCurrencies = new List<string> { "AUD", "GBP", "NZD" };
            var currencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)?.CurrencyCode ?? string.Empty;
            var result = supportedCurrencies.Contains(currencyCode, StringComparer.InvariantCultureIgnoreCase);

            return (result, currencyCode);
        }

        /// <summary>
        /// Prepare price breakdown
        /// </summary>
        /// <param name="priceValue">Price value</param>
        /// <returns>Result; Formatted first and regular prices</returns>
        public (bool Result, string InitialPrice, string Price) PreparePriceBreakdown(decimal? priceValue = null)
        {
            //whether the store currency is supported
            var (currencySupported, currencyCode) = PrimaryStoreCurrencySupported();
            if (!currencySupported)
                return (false, default, default);

            //get price value
            if (!priceValue.HasValue)
            {
                var cart = _shoppingCartService
                    .GetShoppingCart(_workContext.CurrentCustomer, ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
                if (cart.Any())
                {
                    var cartTotal = _orderTotalCalculationService.GetShoppingCartTotal(cart) ?? decimal.Zero;
                    priceValue = _currencyService.ConvertFromPrimaryStoreCurrency(cartTotal, _workContext.WorkingCurrency);
                }
            }

            if (!priceValue.HasValue || priceValue == decimal.Zero)
                return (false, default, default);

            //whether to use Laybuy Boost price breakdown
            var priceLimit = decimal.MaxValue;
            var firstPrice = decimal.Zero;
            if (currencyCode.Equals("AUD", StringComparison.InvariantCultureIgnoreCase) ||
                currencyCode.Equals("NZD", StringComparison.InvariantCultureIgnoreCase))
            {
                priceLimit = 1440M;
                firstPrice = 240M;
            }

            if (currencyCode.Equals("GBP", StringComparison.InvariantCultureIgnoreCase))
            {
                priceLimit = 720M;
                firstPrice = 120M;
            }

            //prepare prices
            var initialPrice = string.Empty;
            var priceInPrimaryCurrency = _currencyService.ConvertToPrimaryStoreCurrency(priceValue.Value, _workContext.WorkingCurrency);
            if (priceInPrimaryCurrency > priceLimit)
            {
                var initialPriceValue = _currencyService
                    .ConvertFromPrimaryStoreCurrency(firstPrice + (priceInPrimaryCurrency - priceLimit), _workContext.WorkingCurrency);
                initialPrice = _priceFormatter.FormatPrice(initialPriceValue, true, false);
                priceValue = _currencyService.ConvertFromPrimaryStoreCurrency(firstPrice, _workContext.WorkingCurrency);
            }
            else
                priceValue /= 6;
            var price = priceValue > decimal.Zero ? _priceFormatter.FormatPrice(priceValue.Value, true, false) : string.Empty;

            return (true, initialPrice, price);
        }

        /// <summary>
        /// Create order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="returnUrl">URL to redirect the customer to once the payment process is completed</param>
        /// <returns>Response; error message if exists</returns>
        public (CreateResponse Response, string ErrorMessage) CreateOrder(Order order, string returnUrl)
        {
            return HandleFunction(() =>
            {
                if (order == null)
                    throw new NopException("Order cannot be loaded");

                var customer = _customerService.GetCustomerById(order.CustomerId);
                if (customer == null)
                    throw new NopException("Customer cannot be loaded");

                var billingAddress = _addressService.GetAddressById(order.BillingAddressId);
                if (billingAddress == null)
                    throw new NopException("Billing address cannot be loaded");

                //prepare request to create new order
                var request = new CreateRequest
                {
                    TotalAmount = order.OrderTotal,
                    Currency = PrimaryStoreCurrencySupported().CurrencyCode,
                    ReturnUrl = returnUrl,
                    MerchantReference = order.CustomOrderNumber,
                    TaxAmount = order.OrderTax
                };

                //set customer details
                request.Customer = new CustomerDetails
                {
                    FirstName = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.FirstNameAttribute),
                    LastName = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.LastNameAttribute),
                    Email = customer.Email,
                    Phone = _genericAttributeService.GetAttribute<string>(customer, NopCustomerDefaults.PhoneAttribute)
                };

                //billing address details
                request.BillingAddress = new AddressDetails
                {
                    Name = $"{billingAddress.FirstName} {billingAddress.LastName}",
                    Phone = billingAddress.PhoneNumber,
                    AddressLine1 = billingAddress.Address1,
                    AddressLine2 = billingAddress.Address2,
                    City = billingAddress.City,
                    Suburb = billingAddress.County,
                    State = _stateProvinceService.GetStateProvinceById(billingAddress.StateProvinceId ?? 0)?.Name,
                    PostalCode = billingAddress.ZipPostalCode,
                    Country = _countryService.GetCountryById(billingAddress.CountryId ?? 0)?.Name
                };

                //shipping address details
                var shippingAddress = _addressService.GetAddressById(order.PickupAddressId ?? order.ShippingAddressId ?? 0);
                if (shippingAddress != null)
                {
                    request.ShippingAddress = new AddressDetails
                    {
                        Name = $"{shippingAddress.FirstName} {shippingAddress.LastName}",
                        Phone = shippingAddress.PhoneNumber,
                        AddressLine1 = shippingAddress.Address1,
                        AddressLine2 = shippingAddress.Address2,
                        City = shippingAddress.City,
                        Suburb = shippingAddress.County,
                        State = _stateProvinceService.GetStateProvinceById(shippingAddress.StateProvinceId ?? 0)?.Name,
                        PostalCode = shippingAddress.ZipPostalCode,
                        Country = _countryService.GetCountryById(shippingAddress.CountryId ?? 0)?.Name
                    };
                }

                //purchased items details
                request.Items = PrepareOrderItems(order);

                return HandleRequest<CreateRequest, CreateResponse>(request);
            });
        }

        /// <summary>
        /// Refund order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amount">Amount to refund</param>
        /// <returns>Response; error message if exists</returns>
        public (RefundResponse Response, string ErrorMessage) RefundOrder(Order order, decimal amount)
        {
            return HandleFunction(() =>
            {
                var request = new RefundRequest
                {
                    OrderId = _genericAttributeService.GetAttribute<int?>(order, LaybuyDefaults.OrderId),
                    Amount = amount
                };
                return HandleRequest<RefundRequest, RefundResponse>(request);
            });
        }

        /// <summary>
        /// Confirm order
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <returns>Result; error message if exists</returns>
        public (bool Result, string ErrorMessage) ConfirmOrder(int orderId)
        {
            return HandleFunction(() =>
            {
                //try to get an order for transaction
                var order = _orderService.GetOrderById(orderId)
                    ?? throw new NopException("Order cannot be loaded");

                //check the status
                var statusValue = _webHelper.QueryString<string>("status");
                if (!Enum.TryParse<ResponseResult>(statusValue, true, out var status) || status != ResponseResult.Success)
                    throw new NopException($"Order is {statusValue}");

                //validate received transaction
                var orderToken = _genericAttributeService.GetAttribute<string>(order, LaybuyDefaults.OrderToken) ?? string.Empty;
                var tokenValue = _webHelper.QueryString<string>("token");
                if (!orderToken.Equals(tokenValue, StringComparison.InvariantCultureIgnoreCase))
                    throw new NopException($"Received order token ({tokenValue}) does not match stored ({orderToken})");

                //order validated, try to confirm
                var request = new ConfirmRequest
                {
                    Token = orderToken,
                    Currency = PrimaryStoreCurrencySupported().CurrencyCode,
                    TotalAmount = order.OrderTotal,
                    Items = PrepareOrderItems(order)
                };
                var response = HandleRequest<ConfirmRequest, ConfirmResponse>(request);
                if (response?.OrderId == null)
                    throw new NopException($"Order identifier not set");

                //order successfully confirmed, mark it as paid
                _genericAttributeService.SaveAttribute<string>(order, LaybuyDefaults.OrderToken, null);
                _genericAttributeService.SaveAttribute(order, LaybuyDefaults.OrderId, response.OrderId.Value);
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    _orderProcessingService.MarkOrderAsPaid(order);

                return true;
            });
        }

        /// <summary>
        /// Check order refunded amount
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <returns>Order; error message if exists</returns>
        public (Order order, string ErrorMessage) CheckRefunds(int orderId)
        {
            return HandleFunction(() =>
            {
                var order = _orderService.GetOrderById(orderId)
                    ?? throw new NopException("Order cannot be loaded");

                var request = new GetRequest
                {
                    MerchantReference = order.CustomOrderNumber
                };
                var response = HandleRequest<GetRequest, GetResponse>(request);

                //check refunds
                var refundedAmount = response.Refunds?.Sum(refund => refund.Amount);
                if (!refundedAmount.HasValue || refundedAmount.Value == order.RefundedAmount)
                    return null;

                //clarify refunded amount
                order.RefundedAmount = refundedAmount.Value;
                _orderService.UpdateOrder(order);
                return order;
            });
        }

        #endregion
    }
}