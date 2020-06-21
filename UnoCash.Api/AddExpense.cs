using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;
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
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post")]
            HttpRequest req,
            ILogger log) =>
            req.Tap(_ => log.LogInformation("Adding a new expense"))
               .Body
               .TMap(body => new StreamReader(body))
               .UsingAsync(r => r.ReadToEndAsync())
               .TTap(body => log.LogInformation($"Request body: {body}"))
               .Map(JsonConvert.DeserializeObject<Expense>)
               .TTap(expense => new[]
                                {
                                    expense.Account,
                                    expense.Date.ToString(CultureInfo.InvariantCulture),
                                    expense.Payee,
                                    expense.Status,
                                    expense.Type,
                                    expense.Description,
                                    expense.Tags
                                }.Iter(x => log.LogWarning(x)))
               .Bind(expense => expense.WriteAsync(req.GetUserUpn()))
               .Map(isSuccessful => isSuccessful ?
                                    (IActionResult)new OkResult() :
                                    new InternalServerErrorResult());
    }
}
