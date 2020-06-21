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
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "delete")]
            HttpRequest req,
            ILogger log) =>
            req.Tap(_ => log.LogInformation("Deleting expense"))
               .Query
               .ExtractSingleGuidValue("id")
               .Bind(expenseId => req.Query
                                     .ExtractSingleStringValue("account")
                                     .Map(account => (expenseId, account)))
               .RTap(t => log.LogWarning($"Deleting expense with ID: {t.expenseId} from {t.account}"))
               .Match(t => ExpenseWriter.DeleteAsync(t.account, req.GetUserUpn(), t.expenseId)
                                        .MatchAsync(() => new OkResult(),
                                                    () => (IActionResult)new NotFoundResult()),
                      e => new BadRequestErrorMessageResult(e).ToTask<IActionResult>());
    }
}
