using Newtonsoft.Json;

namespace Nop.Plugin.Payments.Laybuy.Domain
{
    /// <summary>
    /// Represents address details
    /// </summary>
    public class AddressDetails
    {
        /// <summary>
        /// Gets or sets the contact's full name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the contact's phone number
        /// </summary>
        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        /// <summary>
        /// Gets or sets the first line of the address
        /// </summary>
        [JsonProperty(PropertyName = "address1")]
        public string AddressLine1 { get; set; }

        /// <summary>
        /// Gets or sets the second line of the address
        /// </summary>
        [JsonProperty(PropertyName = "address2")]
        public string AddressLine2 { get; set; }

        /// <summary>
        /// Gets or sets the suburb of the address
        /// </summary>
        [JsonProperty(PropertyName = "suburb")]
        public string Suburb { get; set; }

        /// <summary>
        /// Gets or sets the city of the address
        /// </summary>
        [JsonProperty(PropertyName = "city")]
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the state of the address
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the postcode of the address
        /// </summary>
        [JsonProperty(PropertyName = "postcode")]
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country of the address
        /// </summary>
        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}