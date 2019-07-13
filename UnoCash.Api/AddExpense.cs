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
using UnoCash.Shared;

namespace UnoCash.Api
{
    public static class AddExpense
    {
        [FunctionName(Constants.AddFunction)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Adding a new expense");

            var body =
                await new StreamReader(req.Body).UsingAsync(r => r.ReadToEndAsync());

            log.LogInformation($"Request body: {body}");

            var expense =
                JsonConvert.DeserializeObject<Expense>(body);

            new[]
            {
                expense.Account,
                expense.Date.ToString(CultureInfo.InvariantCulture),
                expense.Payee,
                expense.Status,
                expense.Type,
                expense.Description,
            }.Iter(x => log.LogWarning(x));

            expense.Write();

            return new OkObjectResult("Expense successfully added");
        }
    }
}
