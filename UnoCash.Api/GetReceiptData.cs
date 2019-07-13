using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace UnoCash.Api
{
    public static class GetReceiptData
    {
        [FunctionName("GetReceiptData")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            var blob = req.Query["blobName"];

            log.LogWarning($"Processing the receipt for {blob}");

            return new OkObjectResult(new
            {
                Payee = "Tesco",
                Date = DateTime.Today.AddDays(-400),
                Method = "Cash",
                Amount = 12.12M
            });
        }
    }
}
