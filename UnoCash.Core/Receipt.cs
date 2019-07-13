using System;

namespace UnoCash.Core
{
    public class Receipt
    {
        public string Payee { get; set; }
        public DateTime Date { get; set; }
        public string Method { get; set; }
        public decimal Amount { get; set; }
    }
}