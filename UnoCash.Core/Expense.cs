using System;

namespace UnoCash.Core
{
    public class Expense
    {
        public decimal Amount { get; set; }
        public string Description { get; set; }
        public DateTime Date { get; set; }
        public string Account { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public int Id { get; set; }
    }
}