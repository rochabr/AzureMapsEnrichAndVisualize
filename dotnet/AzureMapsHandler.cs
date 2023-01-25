using Azure;
using Azure.Maps.Search;
using Azure.Maps.Search.Models;
using System;
using System.Threading.Tasks;

namespace Microsoft.AzureMaps
{
    public sealed class AzureMapsHandler
    {
        private static int counter = 0;
        private static AzureMapsHandler instance = null;

        private MapsSearchClient searchClient;
        public static AzureMapsHandler GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AzureMapsHandler();

                    //Get azure maps key from configuration file
                    string azureMapsKey = Environment.GetEnvironmentVariable("AzureMapsKey", EnvironmentVariableTarget.Process);

                    // Create a SearchClient that will authenticate through Subscription Key (Shared key)
                    AzureKeyCredential credential = new AzureKeyCredential(azureMapsKey);
                    instance.searchClient = new MapsSearchClient(credential);
                }
                return instance;
            }
        }

        private AzureMapsHandler()
        {
            counter++;

        }
        public async Task<Location> SearchForAddress(Location location)
        {
            var address = new StructuredAddress
            {
                CountryCode = location.country_code,
                StreetNumber = location.street_number,
                StreetName = location.street_name,
                Municipality = location.city,
                CountrySubdivision = location.province,
                PostalCode = location.postal_code
            };

            Response<SearchAddressResult> searchResult = await instance.searchClient.SearchStructuredAddressAsync(address);

            SearchAddressResultItem resultItem = searchResult.Value.Results[0];

            location.latitude = resultItem.Position.Latitude;
            location.longitude = resultItem.Position.Longitude;
            return location;
        }
    }
}