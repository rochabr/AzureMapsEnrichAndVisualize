using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace Microsoft.AzureMaps
{
    public static class GetLocations
    {
        [FunctionName("GetLocations")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "locations")]
                HttpRequest req, ILogger log,
            [Sql("select * from Locations",
                CommandType = System.Data.CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
            IEnumerable<Location> location)
        {
            return new OkObjectResult(location);
        }
    }
}
