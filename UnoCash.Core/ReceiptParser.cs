using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using CloudStorageAccount = Microsoft.Azure.Storage.CloudStorageAccount;

namespace UnoCash.Core
{
    public static class ReceiptParser
    {
        public static async Task<Receipt> ParseAsync(string blobName)
        {
            var client =
                CloudStorageAccount.DevelopmentStorageAccount
                                   .CreateCloudBlobClient();

            var container = client.GetContainerReference("receipts");

            var blob = container.GetBlobReference(blobName);

            //blob.Uri

            var http = new HttpClient();

            const string fre = "FormRecognizerEndpoint";

            var endpoint = await ConfigurationReader.GetAsync(fre)
                                                    .ConfigureAwait(false);

            var url =
                $"https://{endpoint}/formrecognizer/" +
                "v1.0-preview/prebuilt/receipt/asyncBatchAnalyze";

            var blobStream = new MemoryStream();

            await blob.DownloadToStreamAsync(blobStream)
                      .ConfigureAwait(false);

            string hash;

            using (var md5 = MD5.Create())
                hash = Convert.ToBase64String(md5.ComputeHash(blobStream));

            var existing =
                await AzureTableStorage.GetAllAsync("receipthashes")
                                       .ConfigureAwait(false);

            var entity = 
                existing.SingleOrDefault(x => x.PartitionKey == "receipts" &&
                                              x.RowKey == hash);

            if (entity != null)
            {
                var res = entity.Properties["Results"].StringValue;

                var text =
                    await container.GetBlockBlobReference(res)
                                   .DownloadTextAsync()
                                   .ConfigureAwait(false);

                return text.ToReceipt();
            }

            blobStream.Seek(0, SeekOrigin.Begin);

            var streamContent = new StreamContent(blobStream);

            streamContent.Headers.ContentType =
                new MediaTypeHeaderValue("application/octet-stream");

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = streamContent
            };

            const string frk = "FormRecognizerKey";

            http.DefaultRequestHeaders
                .Add("Ocp-Apim-Subscription-Key", await SecretReader.GetAsync(frk)
                                                                    .ConfigureAwait(false));

            var response = await http.SendAsync(request)
                                     .ConfigureAwait(false);

            var content =
                await response.Content
                              .ReadAsStringAsync()
                              .ConfigureAwait(false);

            Console.WriteLine(content);

            var location =
                response.Headers.TryGetValues("Operation-Location", out var values) ?
                values.Single() :
                throw new Exception();

            response.EnsureSuccessStatusCode();

            bool resultAvailable;

            string responseContent;

            do
            {
                var result =
                    await http.GetAsync(location)
                              .ConfigureAwait(false);

                responseContent =
                    await result.Content
                                .ReadAsStringAsync()
                                .ConfigureAwait(false);

                var bodyResult =
                    JsonConvert.DeserializeObject<ReceiptRecognizerResult>(responseContent);

                resultAvailable = bodyResult.Status == "Succeeded";

            } while (!resultAvailable);

            var resultsBlobGuid = Guid.NewGuid().ToString();

            await container.GetBlockBlobReference(resultsBlobGuid)
                           .UploadTextAsync(responseContent)
                           .ConfigureAwait(false);

            SaveResultsToCache(hash, resultsBlobGuid);

            return responseContent.ToReceipt();
        }

        static void SaveResultsToCache(string hash, string results) =>
            new DynamicTableEntity("receipts",
                                   hash,
                                   "*",
                                   results.ToPropertiesDictionary())
                .Write("receipthashes");

        static Dictionary<string, EntityProperty> ToPropertiesDictionary(this string results) =>
            new Dictionary<string, EntityProperty>
            {
                ["Results"] = EntityProperty.GeneratePropertyForString(results)
            };

        static Receipt ToReceipt(this string recognizerResult) =>
            JsonConvert.DeserializeObject<ReceiptRecognizerResult>(recognizerResult)
                       .UnderstandingResults
                       .Single()
                       .Fields
                       .ToReceipt();

        static Receipt ToReceipt(this Fields fields) =>
            new Receipt
            {
                Payee = fields.MerchantName.Value,
                Date  = fields.TransactionDate.Value,
                //Method = "Cash",
                Amount = fields.Total.Value
            };
    }

    static class PersistentCache
    {

    }
}
