using System;

namespace UnoCash.Core
{
    public class Expense
    {
        public decimal Amount { get; set; }
        public string Payee { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public string Tags { get; set; }
        public Guid Id { get; set; }
    }
}