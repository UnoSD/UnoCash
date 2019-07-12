using System;
using System.Linq;
using Microsoft.Azure.Cosmos.Table;

namespace UnoCash.Core
{
    public static class ExpenseReader
    {
        public static Expense[] Get(string account, Guid id) =>
            GetAll(account).Where(expense => expense.Id == id)
                           .ToArray();

        public static Expense[] GetAll(string account) =>
            AzureTableStorage.GetAll(nameof(Expense))
                             .Select(ToExpense)
                             .Where(expense => expense.Account == account)
                             .ToArray();

        static Expense ToExpense(this DynamicTableEntity expense) =>
            new Expense
            {
                Id = Guid.Parse(expense.RowKey),
                Account = expense.Properties[nameof(Expense.Account)].StringValue,
                Description = expense.Properties[nameof(Expense.Description)].StringValue,
                Status = expense.Properties[nameof(Expense.Status)].StringValue,
                Type = expense.Properties[nameof(Expense.Type)].StringValue,
                Date = expense.Properties[nameof(Expense.Date)].DateTime ?? throw new Exception(),
                Amount = expense.Properties[nameof(Expense.Amount)].Int64Value / 100m ?? throw new Exception()
            };
    }
}