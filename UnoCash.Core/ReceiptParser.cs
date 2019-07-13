using System;
using System.Threading.Tasks;

namespace UnoCash.Core
{
    public class ReceiptParser
    {
        public static async Task<Receipt> ParseAsync(string blobName) =>
            new Receipt
            {
                Payee  = "Tesco",
                Date   = DateTime.Today.AddDays(-400),
                Method = "Cash",
                Amount = 12.12M
            };
    }
}
