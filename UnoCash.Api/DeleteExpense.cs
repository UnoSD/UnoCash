using System;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
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
               .ExtractExpenseId()
               .Bind(expenseId => req.Query
                                     .ExtractSingleStringValue("account")
                                     .Map(account => (expenseId, account)))
               .RTap(t => log.LogWarning($"Deleting expense with ID: {t.expenseId} from {t.account}"))
               .Match(t => ExpenseWriter.DeleteAsync(t.account, t.expenseId)
                                        .MatchAsync(() => new OkObjectResult("Expense deleted"),
                                                    () => (IActionResult)new NotFoundResult()),
                      e => new BadRequestErrorMessageResult(e).ToTask<IActionResult>());

        static IResult<Guid> ExtractExpenseId(this IQueryCollection req) =>
            req.ExtractSingleStringValue("id")
               .Bind(value => Guid.TryParse(value, out var guid)
                                  .Match(() => guid.Success(),
                                         () => $"Could not parse the ID: {value}".Failure<Guid>()));

        static IResult<string> ExtractSingleStringValue(this IQueryCollection col, string key) =>
            col.TryGetValue(key, out var stringValues)
               .Match(() => stringValues.Success(),
                      () => $"Cannot find query parameter {key}".Failure<StringValues>())
               .Bind(values => values.Match(()    => $"Missing value for {key}".Failure<string>(),
                                            value => value.Success(),
                                            _     => $"Too many values for {key}".Failure<string>()));
    }


}
