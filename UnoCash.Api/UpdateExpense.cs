using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UnoCash.Core;

namespace UnoCash.Api
{
    public static class UpdateExpense
    {
        // Upsert and delete add?
        [FunctionName("UpdateExpense")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "patch", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var expense = JsonConvert.DeserializeObject<Expense>(requestBody);

            log.LogWarning($"Updated expense with ID: {expense.Id}");

            return new OkObjectResult("");
        }
    }
}
