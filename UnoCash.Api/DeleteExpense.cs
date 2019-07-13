using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UnoCash.Core;
using UnoCash.Shared;

namespace UnoCash.Api
{
    public static class DeleteExpense
    {
        // Take function name from constant and use the same in the front end URL
        [FunctionName(Constants.DeleteExpense)]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete")]
            HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Deleting expense");

            var id = req.Query["id"].SingleOrDefault();

            if (!Guid.TryParse(id, out var guid))
                return new BadRequestErrorMessageResult($"Invalid guid provided: {id ?? "null"}");

            var account = req.Query["account"].SingleOrDefault();

            if (string.IsNullOrEmpty(account))
                return new BadRequestErrorMessageResult("Missing or empty account provided");

            log.LogWarning($"Deleting expense with ID: {guid}");

            return await ExpenseWriter.DeleteAsync(account, guid) ?
                   (IActionResult)new OkObjectResult("Expense deleted") :
                   new NotFoundResult();
        }
    }
}
