using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace UnoCash.Api
{
    public static class DeleteExpense
    {
        [FunctionName("DeleteExpense")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            var id = int.Parse(req.Query["id"]);

            log.LogWarning($"Deleting expense with ID: {id}");

            return new OkObjectResult("");
        }
    }
}
