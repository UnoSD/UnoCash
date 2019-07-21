using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using UnoCash.Core;

namespace UnoCash.Api
{
    public static class GetReceiptUploadSasToken
    {
        [FunctionName(nameof(GetReceiptUploadSasToken))]
        public static Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log)
        {
            log.LogWarning("Getting blob upload SAS token for receipts container");

            const string token =
                "?st=2019-07-13T10%3A01%3A33Z&se=2026-07-14T10%3A01%3A00Z&" +
                "sp=racwdl&sv=2018-03-28&" +
                "sr=c&" +
                "sig=F1jpNFt4H0ujsGqCaeIZiwWwKXEAV7YE4WPxhkvcd4A%3D";

            return new OkObjectResult(token).ToTask<IActionResult>();
        }
    }
}
