using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace UnoCash.Core
{
    public static class ExpenseWriter
    {
        public static Task<bool> WriteAsync(this Expense expense, string email) =>
            expense.ToTableEntity(email)
                   .WriteAsync(nameof(Expense));

        static DynamicTableEntity ToTableEntity(this Expense expense, string email) =>
            new DynamicTableEntity(expense.Account + email,
                expense.Id.Coalesce(Guid.NewGuid()).ToString(),
                "*",
                new Dictionary<string, EntityProperty>
                {
                    [nameof(Expense.Account)] = EntityProperty.GeneratePropertyForString(expense.Account),
                    [nameof(Expense.Payee)] = EntityProperty.GeneratePropertyForString(expense.Payee),
                    [nameof(Expense.Description)] = EntityProperty.GeneratePropertyForString(expense.Description),
                    [nameof(Expense.Status)] = EntityProperty.GeneratePropertyForString(expense.Status),
                    [nameof(Expense.Type)] = EntityProperty.GeneratePropertyForString(expense.Type),
                    [nameof(Expense.Date)] = EntityProperty.GeneratePropertyForDateTimeOffset(expense.Date),
                    [nameof(Expense.Amount)] = EntityProperty.GeneratePropertyForLong((long)(expense.Amount * 100L)),
                    [nameof(Expense.Tags)] = EntityProperty.GeneratePropertyForString(expense.Tags)
                });

        public static Task<bool> DeleteAsync(string account, string email, Guid id) => 
            AzureTableStorage.DeleteAsync(nameof(Expense), account + email, id.ToString());
    }
}
