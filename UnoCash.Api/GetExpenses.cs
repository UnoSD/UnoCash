using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UnoCash.Core;

namespace UnoCash.Api
{
    public static class GetExpenses
    {
        [FunctionName("GetExpenses")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            var account = req.Query["account"];

            var email = req.GetUserUpn();

            log.LogWarning($"Fetching expense(s) for account: {account}, user: {email}");

            return new OkObjectResult(Guid.TryParse(req.Query["id"], out var id) ?
                                          await ExpenseReader.GetAsync(account, email, id):
                                          await ExpenseReader.GetAllAsync(account, email));
        }
    }
}