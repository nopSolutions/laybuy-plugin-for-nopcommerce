using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nop.Core;
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
            return _laybuySettings.UseSandbox ||
                !string.IsNullOrEmpty(_laybuySettings.MerchantId) && !string.IsNullOrEmpty(_laybuySettings.AuthenticationKey);
        }

        /// <summary>
        /// Handle function and get result
        /// </summary>
        /// <typeparam name="TResult">Result type</typeparam>
        /// <param name="function">Function</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the function result; error message if exists</returns>
        private async Task<(TResult Result, string ErrorMessage)> HandleFunctionAsync<TResult>(Func<Task<TResult>> function)
        {
            try
            {
                //ensure that plugin is configured
                if (!IsConfigured())
                    throw new NopException("Plugin not configured");

                //invoke function
                return (await function(), default);
            }
            catch (Exception exception)
            {
                //log errors
                var customer = await _workContext.GetCurrentCustomerAsync();
                var errorMessage = $"{LaybuyDefaults.SystemName} error: {Environment.NewLine}{exception.Message}";
                await _logger.ErrorAsync(errorMessage, exception, customer);

                return (default, errorMessage);
            }
        }

        /// <summary>
        /// Handle request and get response
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the response</returns>
        private async Task<TResponse> HandleRequestAsync<TRequest, TResponse>(TRequest request)
            where TRequest : Request where TResponse : Response
        {
            //execute request
            var response = await _httpClient.RequestAsync<TRequest, TResponse>(request)
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
        /// <returns>A task that represents the asynchronous operation whose result contains the item details</returns>
        private async Task<List<ItemDetails>> PrepareOrderItemsAsync(Order order)
        {
            var items = new List<ItemDetails>();

            //add purchased items
            var orderItems = await _orderService.GetOrderItemsAsync(order.Id);
            foreach (var item in orderItems)
            {
                var product = await _productService.GetProductByIdAsync(item.ProductId);
                var sku = product != null
                    ? await _productService.FormatSkuAsync(product, item.AttributesXml)
                    : string.Empty;
                items.Add(new ItemDetails
                {
                    ItemId = !string.IsNullOrEmpty(sku) ? sku : product?.Id.ToString(),
                    Description = product?.Name,
                    Price = item.UnitPriceExclTax,
                    Quantity = item.Quantity
                });
            }

            //add checkout attributes as order items
            var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId);
            var checkoutAttributeValues = _checkoutAttributeParser.ParseCheckoutAttributeValues(order.CheckoutAttributesXml);
            await foreach (var (attribute, values) in checkoutAttributeValues)
            {
                await foreach (var attributeValue in values)
                {
                    var (price, _) = await _taxService.GetCheckoutAttributePriceAsync(attribute, attributeValue, false, customer);
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
        /// <returns>A task that represents the asynchronous operation whose result contains the check result; primary store currency code</returns>
        public async Task<(bool Result, string CurrencyCode)> IsPrimaryStoreCurrencySupportedAsync()
        {
            var (result, _) = await HandleFunctionAsync(async () =>
            {
                //New Zealand Dollars (NZD), Australian Dollars (AUD) and British Pound (GBP) are currently the only currencies supported
                var supportedCurrencies = new List<string> { "AUD", "GBP", "NZD" };
                var currency = await _currencyService.GetCurrencyByIdAsync(_currencySettings.PrimaryStoreCurrencyId);
                var currencyCode = currency?.CurrencyCode ?? string.Empty;
                var result = supportedCurrencies.Contains(currencyCode, StringComparer.InvariantCultureIgnoreCase);

                return (result, currencyCode);
            });

            return result;
        }

        /// <summary>
        /// Prepare price breakdown
        /// </summary>
        /// <param name="priceValue">Price value</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the check result; formatted first and regular prices</returns>
        public async Task<(bool Result, string InitialPrice, string Price)> PreparePriceBreakdownAsync(decimal? priceValue = null)
        {
            var (result, _) = await HandleFunctionAsync(async () =>
            {
                //whether the store currency is supported
                var (currencySupported, currencyCode) = await IsPrimaryStoreCurrencySupportedAsync();
                if (!currencySupported)
                    return (false, default, default);

                var customer = await _workContext.GetCurrentCustomerAsync();
                var store = await _storeContext.GetCurrentStoreAsync();
                var currency = await _workContext.GetWorkingCurrencyAsync();

                //get price value
                if (!priceValue.HasValue)
                {
                    var cart = await _shoppingCartService.GetShoppingCartAsync(customer, ShoppingCartType.ShoppingCart, store.Id);
                    if (cart.Any())
                    {
                        var (cartTotal, _, _, _, _, _) = await _orderTotalCalculationService
                            .GetShoppingCartTotalAsync(cart, usePaymentMethodAdditionalFee: false);
                        if (!cartTotal.HasValue)
                        {
                            var (_, _, _, subTotal, _) = await _orderTotalCalculationService.GetShoppingCartSubTotalAsync(cart, true);
                            cartTotal = subTotal;
                        }
                        priceValue = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(cartTotal ?? decimal.Zero, currency);
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
                var priceInPrimaryCurrency = await _currencyService.ConvertToPrimaryStoreCurrencyAsync(priceValue.Value, currency);
                if (priceInPrimaryCurrency > priceLimit)
                {
                    var initialPriceValue = await _currencyService
                        .ConvertFromPrimaryStoreCurrencyAsync(firstPrice + (priceInPrimaryCurrency - priceLimit), currency);
                    initialPrice = await _priceFormatter.FormatPriceAsync(initialPriceValue, true, false);
                    priceValue = await _currencyService.ConvertFromPrimaryStoreCurrencyAsync(firstPrice, currency);
                }
                else
                    priceValue /= 6;
                var price = priceValue > decimal.Zero
                    ? await _priceFormatter.FormatPriceAsync(priceValue.Value, true, false)
                    : string.Empty;

                return (true, initialPrice, price);
            });

            return result;
        }

        /// <summary>
        /// Create order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="returnUrl">URL to redirect the customer to once the payment process is completed</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the response; error message if exists</returns>
        public async Task<(CreateResponse Response, string ErrorMessage)> CreateOrderAsync(Order order, string returnUrl)
        {
            return await HandleFunctionAsync(async () =>
            {
                if (order == null)
                    throw new NopException("Order cannot be loaded");

                var customer = await _customerService.GetCustomerByIdAsync(order.CustomerId)
                    ?? throw new NopException("Customer cannot be loaded");

                var billingAddress = await _addressService.GetAddressByIdAsync(order.BillingAddressId)
                    ?? throw new NopException("Billing address cannot be loaded");

                //prepare request to create new order
                var (_, currencyCode) = await IsPrimaryStoreCurrencySupportedAsync();
                var request = new CreateRequest
                {
                    TotalAmount = order.OrderTotal,
                    Currency = currencyCode,
                    ReturnUrl = returnUrl,
                    MerchantReference = order.CustomOrderNumber,
                    TaxAmount = order.OrderTax
                };

                //set customer details 
                //we need to pull this info from the actual checkout process (not from the account), so let's use billing address info
                request.Customer = new CustomerDetails
                {
                    FirstName = billingAddress.FirstName,
                    LastName = billingAddress.LastName,
                    Email = billingAddress.Email,
                    Phone = billingAddress.PhoneNumber
                };

                //billing address details
                var country = await _countryService.GetCountryByIdAsync(billingAddress.CountryId ?? 0);
                var state = await _stateProvinceService.GetStateProvinceByIdAsync(billingAddress.StateProvinceId ?? 0);
                request.BillingAddress = new AddressDetails
                {
                    Name = $"{billingAddress.FirstName} {billingAddress.LastName}",
                    Phone = billingAddress.PhoneNumber,
                    AddressLine1 = billingAddress.Address1,
                    AddressLine2 = billingAddress.Address2,
                    City = billingAddress.City,
                    Suburb = billingAddress.County,
                    State = state?.Name,
                    PostalCode = billingAddress.ZipPostalCode,
                    Country = country?.Name
                };

                //shipping address details
                var shippingAddress = await _addressService.GetAddressByIdAsync(order.PickupAddressId ?? order.ShippingAddressId ?? 0);
                if (shippingAddress != null)
                {
                    var shippingCountry = await _countryService.GetCountryByIdAsync(shippingAddress.CountryId ?? 0);
                    var shippingState = await _stateProvinceService.GetStateProvinceByIdAsync(shippingAddress.StateProvinceId ?? 0);
                    request.ShippingAddress = new AddressDetails
                    {
                        Name = $"{shippingAddress.FirstName} {shippingAddress.LastName}",
                        Phone = shippingAddress.PhoneNumber,
                        AddressLine1 = shippingAddress.Address1,
                        AddressLine2 = shippingAddress.Address2,
                        City = shippingAddress.City,
                        Suburb = shippingAddress.County,
                        State = shippingState?.Name,
                        PostalCode = shippingAddress.ZipPostalCode,
                        Country = shippingCountry?.Name
                    };
                }

                //purchased items details
                request.Items = await PrepareOrderItemsAsync(order);

                return await HandleRequestAsync<CreateRequest, CreateResponse>(request);
            });
        }

        /// <summary>
        /// Refund order
        /// </summary>
        /// <param name="order">Order</param>
        /// <param name="amount">Amount to refund</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the response; error message if exists</returns>
        public async Task<(RefundResponse Response, string ErrorMessage)> RefundOrderAsync(Order order, decimal amount)
        {
            return await HandleFunctionAsync(async () =>
            {
                var orderId = await _genericAttributeService.GetAttributeAsync<int?>(order, LaybuyDefaults.OrderId);
                var request = new RefundRequest
                {
                    OrderId = orderId,
                    Amount = amount
                };

                return await HandleRequestAsync<RefundRequest, RefundResponse>(request);
            });
        }

        /// <summary>
        /// Confirm order
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the check result; error message if exists</returns>
        public async Task<(bool Result, string ErrorMessage)> ConfirmOrderAsync(int orderId)
        {
            return await HandleFunctionAsync(async () =>
            {
                //try to get an order for transaction
                var order = await _orderService.GetOrderByIdAsync(orderId)
                    ?? throw new NopException("Order cannot be loaded");

                //check the status
                var statusValue = _webHelper.QueryString<string>("status");
                if (!Enum.TryParse<ResponseResult>(statusValue, true, out var status) || status != ResponseResult.Success)
                    throw new NopException($"Order is {statusValue}");

                //validate received transaction
                var orderToken = await _genericAttributeService.GetAttributeAsync<string>(order, LaybuyDefaults.OrderToken) ?? string.Empty;
                var tokenValue = _webHelper.QueryString<string>("token");
                if (!orderToken.Equals(tokenValue, StringComparison.InvariantCultureIgnoreCase))
                    throw new NopException($"Received order token ({tokenValue}) does not match stored ({orderToken})");

                //order validated, try to confirm
                var (_, currencyCode) = await IsPrimaryStoreCurrencySupportedAsync();
                var items = await PrepareOrderItemsAsync(order);
                var request = new ConfirmRequest
                {
                    Token = orderToken,
                    Currency = currencyCode,
                    TotalAmount = order.OrderTotal,
                    Items = items
                };
                var response = await HandleRequestAsync<ConfirmRequest, ConfirmResponse>(request);
                if (response?.OrderId == null)
                    throw new NopException($"Order identifier not set");

                //order successfully confirmed, mark it as paid
                await _genericAttributeService.SaveAttributeAsync<string>(order, LaybuyDefaults.OrderToken, null);
                await _genericAttributeService.SaveAttributeAsync(order, LaybuyDefaults.OrderId, response.OrderId.Value);
                if (_orderProcessingService.CanMarkOrderAsPaid(order))
                    await _orderProcessingService.MarkOrderAsPaidAsync(order);

                return true;
            });
        }

        /// <summary>
        /// Check order refunded amount
        /// </summary>
        /// <param name="orderId">Order identifier</param>
        /// <returns>A task that represents the asynchronous operation whose result contains the order; error message if exists</returns>
        public async Task<(Order order, string ErrorMessage)> CheckRefundsAsync(int orderId)
        {
            return await HandleFunctionAsync(async () =>
            {
                var order = await _orderService.GetOrderByIdAsync(orderId)
                    ?? throw new NopException("Order cannot be loaded");

                var request = new GetRequest
                {
                    MerchantReference = order.CustomOrderNumber
                };
                var response = await HandleRequestAsync<GetRequest, GetResponse>(request);

                //check refunds
                var refundedAmount = response.Refunds?.Sum(refund => refund.Amount);
                if (!refundedAmount.HasValue || refundedAmount.Value == order.RefundedAmount)
                    return null;

                //clarify refunded amount
                order.RefundedAmount = refundedAmount.Value;
                await _orderService.UpdateOrderAsync(order);

                return order;
            });
        }

        #endregion
    }
}