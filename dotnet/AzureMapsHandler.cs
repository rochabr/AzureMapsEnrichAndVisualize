using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using System;
using System.Threading.Tasks;

namespace Microsoft.AzureMaps
{
    public class AzureMapsHandler
    {

        private MapsSearchClient searchClient;

        public AzureMapsHandler()
        {
            //Get azure maps key from configuration file
            string azureMapsKey = Environment.GetEnvironmentVariable("AzureMapsKey", EnvironmentVariableTarget.Process);

            // Create a SearchClient that will authenticate through Subscription Key (Shared key)
            AzureKeyCredential credential = new AzureKeyCredential(azureMapsKey);
            this.searchClient = new MapsSearchClient(credential);
        }

        public async Task<Location> SearchForAddress(Location location)
        {
            //Create Structured address object from database query
            var address = new StructuredAddress
            {
                CountryCode = location.country_code,
                StreetNumber = location.street_number,
                StreetName = location.street_name,
                Municipality = location.city,
                CountrySubdivision = location.province,
                PostalCode = location.postal_code
            };

            //Call the SearchStructuredAddressAsync API to query for the address geolocation
            Response<SearchAddressResult> searchResult = await this.searchClient.SearchStructuredAddressAsync(address);

            SearchAddressResultItem resultItem = searchResult.Value.Results[0];

            location.latitude = resultItem.Position.Latitude;
            location.longitude = resultItem.Position.Longitude;
            return location;
        }
    }
}