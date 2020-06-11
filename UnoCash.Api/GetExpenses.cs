using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
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

            var jwtToken = req.Cookies["jwtToken"];
            log.LogWarning("** Claims **");
            
            var reader = new JwtSecurityTokenHandler();
            reader.ReadJwtToken(jwtToken)
                  .Claims
                  .ToList()
                  .ForEach(c => log.LogWarning($"{c.Value}:" +
                     $"{c.Subject.Name}:" +
                     $"{string.Join("\n", c.Properties.Select(p => $"{p.Key}:{p.Value}"))}"));
            log.LogWarning("** End claims **");

            log.LogWarning($"Fetching expense(s) for account: {account}");

            return new OkObjectResult(Guid.TryParse(req.Query["id"], out var id) ?
                                      await ExpenseReader.GetAsync(account, id):
                                      await ExpenseReader.GetAllAsync(account));
        }
    }
}