using Microsoft.Azure.Cosmos.Table;
using System.Collections.Generic;

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

        internal static IEnumerable<DynamicTableEntity> GetAll(string tableName) => 
            GetOrCreate(tableName).ExecuteQuery(new TableQuery());
    }
}