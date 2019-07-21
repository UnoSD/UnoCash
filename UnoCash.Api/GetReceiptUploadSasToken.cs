using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
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
            ILogger log) =>
            CloudStorageAccount.DevelopmentStorageAccount
                               .Tap(_ => log.LogWarning("Getting blob upload SAS token for receipts container"))
                               .CreateCloudBlobClient()
                               .GetContainerReference("receipts")
                               .GetSharedAccessSignature(new SharedAccessBlobPolicy
                               {
                                   Permissions            = SharedAccessBlobPermissions.Create |
                                                            // Remove, is only to override existing blobs
                                                            SharedAccessBlobPermissions.Write,
                                   SharedAccessExpiryTime = DateTimeOffset.Now.AddMinutes(2)
                                   // Add IP range limit? Other access policy?
                               })
                               .ToOkObject()
                               .ToTask<IActionResult>();
    }
}
