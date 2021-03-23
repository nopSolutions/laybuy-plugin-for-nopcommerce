using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Net.Http.Headers;
using Newtonsoft.Json;
using Nop.Core;
using Nop.Plugin.Payments.Laybuy.Domain;

namespace Nop.Plugin.Payments.Laybuy.Services
{
    /// <summary>
    /// Represents plugin HTTP client
    /// </summary>
    public class LaybuyHttpClient
    {
        #region Fields

        private readonly HttpClient _httpClient;

        #endregion

        #region Ctor

        public LaybuyHttpClient(HttpClient httpClient,
            LaybuySettings laybuySettings)
        {
            //configure client
            httpClient.BaseAddress = new Uri(laybuySettings.UseSandbox ? LaybuyDefaults.SandboxServiceUrl : LaybuyDefaults.ServiceUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(laybuySettings.RequestTimeout ?? 10);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.UserAgent, LaybuyDefaults.UserAgent);
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, MimeTypes.ApplicationJson);

            //set authentication
            var authorization = Convert.ToBase64String(Encoding.Default.GetBytes($"{laybuySettings.MerchantId}:{laybuySettings.AuthenticationKey}"));
            httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, $"Basic {authorization}");

            _httpClient = httpClient;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Request API service
        /// </summary>
        /// <typeparam name="TRequest">Request type</typeparam>
        /// <typeparam name="TResponse">Response type</typeparam>
        /// <param name="request">Request</param>
        /// <returns>The asynchronous task whose result contains response details</returns>
        public async Task<TResponse> RequestAsync<TRequest, TResponse>(TRequest request) where TRequest : Request where TResponse : Response
        {
            try
            {
                //prepare request parameters
                var requestString = JsonConvert.SerializeObject(request);
                var requestContent = new StringContent(requestString, Encoding.Default, MimeTypes.ApplicationJson);

                //execute request and get response
                var requestMessage = new HttpRequestMessage(new HttpMethod(request.Method), request.Path) { Content = requestContent };
                var httpResponse = await _httpClient.SendAsync(requestMessage);

                //return result
                var responseString = await httpResponse.Content.ReadAsStringAsync();
                try
                {
                    return JsonConvert.DeserializeObject<TResponse>(responseString);
                }
                catch (Exception exc)
                {
                    throw new NopException($"Could not recognize response - '{responseString}'", exc);
                }
            }
            catch (AggregateException exception)
            {
                //rethrow actual exception
                throw exception.InnerException;
            }
        }

        #endregion
    }
}