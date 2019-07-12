using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    static class AzureTableStorage
    {
        internal static void Write(this ITableEntity entity, string tableName) => 
            GetOrCreate(tableName).Execute(TableOperation.Insert(entity));

        static CloudTable GetOrCreate(string name)
        {
            var storage = CloudStorageAccount.Parse("UseDevelopmentStorage=true");

            var client = storage.CreateCloudTableClient(new TableClientConfiguration());

            var table = client.GetTableReference(name);

            table.CreateIfNotExists();

            return table;
        }

        internal static Task<IEnumerable<DynamicTableEntity>> GetAllAsync(string tableName) => 
            GetAllAsync(GetOrCreate(tableName), new TableQuery());

        static Task<IEnumerable<DynamicTableEntity>> GetAllAsync(CloudTable table, 
                                                                 TableQuery query) =>
            table.ExecuteQuerySegmented(query, default)
                 .GetAllAsync(table, query);

        static async Task<IEnumerable<DynamicTableEntity>> GetAllAsync(
            this TableQuerySegment<DynamicTableEntity> segment, 
            CloudTable table,
            TableQuery query) =>
            segment.Unfold(ns => (ns.Results, 
                                  table.ExecuteQuerySegmented(query, ns.ContinuationToken)),
                           ns => ns.ContinuationToken == default)
                   .SelectMany(x => x)
                   .Concat(segment.Results)
                   .ToList();
    }
}