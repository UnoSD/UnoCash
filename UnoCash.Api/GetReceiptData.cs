using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UnoCash.Core;

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

            var receipt = await ReceiptParser.ParseAsync(blob);

            return new OkObjectResult(receipt);
        }
    }
}
