using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AzureMaps
{
    public static class EnrichDatabase
    {
        [FunctionName("EnrichDatabase")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "enrich")] HttpRequest req,
            [Sql("select * from Locations where latitude is null and longitude is null", CommandType = System.Data.CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
                IEnumerable<Location> locations,
            [Sql("dbo.Locations", ConnectionStringSetting = "SqlConnectionString")]
                IAsyncCollector<Location> locationsI)

        {
            List<Location> result = new List<Location>();

            //for every location, call SearchForAddress to collect the geolocation and populate the array
            foreach (Location l in locations)
            {
                AzureMapsHandler mapsHandler = new AzureMapsHandler();
                Location location = await mapsHandler.SearchForAddress(l);
                result.Add(location);

                await locationsI.AddAsync(location);
            }

            return new CreatedResult($"/api/enrich", "204");
        }
    }
}