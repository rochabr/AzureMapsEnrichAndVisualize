using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.AzureMaps
{
    public static class GetLocationsGeolocationEmpty
    {
        [FunctionName("GetGeolocationNull")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "geolocation-null")] HttpRequest req,
            [Sql("select * from Locations where latitude is null and longitude is null", CommandType = System.Data.CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
                IEnumerable<Location> locations,
            [Sql("dbo.Locations", ConnectionStringSetting = "SqlConnectionString")]
                IAsyncCollector<Location> locationsI)

        {
            List<Location> result = new List<Location>();

            foreach (Location l in locations)
            {
                AzureMapsHandler mapsHandler = AzureMapsHandler.GetInstance;
                Location location = await mapsHandler.SearchForAddress(l);
                result.Add(location);

                await locationsI.AddAsync(location);
            }

            return new CreatedResult($"/api/geolocation-null", "204");
            //return (ActionResult)new OkObjectResult(locationsI);
        }
    }
}