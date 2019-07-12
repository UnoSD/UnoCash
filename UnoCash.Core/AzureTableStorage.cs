using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;
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

        internal static async Task<IEnumerable<DynamicTableEntity>> GetAllAsync(string tableName)
        {
            var cloudTable = GetOrCreate(tableName);

            var accumulator = new List<DynamicTableEntity>();

            TableQuerySegment<DynamicTableEntity> segment;

            TableContinuationToken tableContinuationToken = default;

            do
            {
                segment = await cloudTable.ExecuteQuerySegmentedAsync(new TableQuery(), tableContinuationToken);

                tableContinuationToken = segment.ContinuationToken;

                accumulator.AddRange(segment.Results);
                
            } while (segment.ContinuationToken != null);

            return accumulator;
        }
    }
}