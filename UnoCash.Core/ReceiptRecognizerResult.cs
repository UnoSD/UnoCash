using System;
using System.Collections.Generic;

namespace UnoCash.Core
{
    class Fields
    {
        public DecimalField Total { get; set; }
        public StringField MerchantName { get; set; }
        public DateField TransactionDate { get; set; }
    }

    class DateField
    {
        public DateTime Value { get; set; }
    }

    class StringField
    {
        public string Value { get; set; }
    }

    class DecimalField
    {
        public decimal Value { get; set; }
        public string Text { get; set; }
    }

    class ReceiptRecognizerResult
    {
        public string Status { get; set; }
        public IReadOnlyCollection<UnderstandingResult> UnderstandingResults { get; set; }
    }

    class UnderstandingResult
    {
        public Fields Fields { get; set; }
    }
}