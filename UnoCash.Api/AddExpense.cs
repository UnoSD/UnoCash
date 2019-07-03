using System.Globalization;
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
            log.LogInformation("Adding a new expense");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            var expense = JsonConvert.DeserializeObject<Expense>(requestBody);

            log.LogInformation(requestBody);

            log.LogWarning(expense.Account);
            log.LogWarning(expense.Status);
            log.LogWarning(expense.Type);
            log.LogWarning(expense.Date.ToString(CultureInfo.InvariantCulture));
            log.LogWarning(expense.Description);

            return new OkObjectResult("Hello");
        }
    }
}
