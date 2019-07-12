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
    public static class DeleteExpense
    {
        // Take function name from constant and use the same in the front end URL
        [FunctionName("DeleteExpense")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            var id = Guid.Parse(req.Query["id"]);
            var account = req.Query["account"];

            log.LogWarning($"Deleting expense with ID: {id}");

            return await ExpenseWriter.DeleteAsync(account, id) ? 
                   (IActionResult)new OkObjectResult("Deleted") : 
                   new NotFoundResult();
        }
    }
}
