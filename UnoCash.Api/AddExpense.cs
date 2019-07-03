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
    public static class AddExpense
    {
        [FunctionName("AddExpense")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var expense = JsonConvert.DeserializeObject<Expense>(requestBody);

            log.LogWarning(requestBody);

            log.LogWarning(expense.Description);

            return new OkObjectResult("Hello");
        }
    }
}
