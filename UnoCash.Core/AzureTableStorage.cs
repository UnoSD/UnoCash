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
            table.ExecuteQuerySegmentedAsync(query, default)
                 .Bind(segment => segment.GetAllAsync(table, query));

        static Task<IEnumerable<DynamicTableEntity>> GetAllAsync(
            this TableQuerySegment<DynamicTableEntity> segment, 
            CloudTable table,
            TableQuery query) =>
            segment.UnfoldAsync(ns => table.ExecuteQuerySegmentedAsync(query, ns.ContinuationToken)
                                           .Map(s => (ns.Results, s)),
                                ns => ns.ContinuationToken == default)
                   .SelectManyAsync(x => x)
                   .ConcatAsync(segment.Results);
    }
}