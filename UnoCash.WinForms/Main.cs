using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;
using Azure.Storage.Blobs;
using UnoCash.Core;
using UnoCash.Dto;
using UnoCash.Shared;
using static UnoCash.Core.ConfigurationKeys;

namespace UnoCash.WinForms
{
    public partial class Main : Form
    {
        readonly HttpClient _httpClient = new HttpClient();

        readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public Main() => InitializeComponent();

        void Main_Load(object sender, EventArgs e)
        {
            accountCbx.DataSource = accountShowCbx.DataSource = new[] { "Current", "ISA", "Wallet" };
            typeCbx.DataSource = new[] { "Regular", "Internal transfer", "Scheduled" };
            statusCbx.DataSource = new[] { "New", "Pending", "Reconciled" };
        }

        async void ViewTab_SelectedIndexChanged(object _, EventArgs __)
        {
            var expensesJson =
                await _httpClient.GetStringAsync("http://localhost:7071/api/GetExpenses?account=Current");

            var expenses = 
                JsonSerializer.Deserialize<Expense[]>(expensesJson, _jsonSerializerOptions);

            expensesLv.Items.AddRange(expenses.Select(e => new ListViewItem(new[]
            {
                e.Date.ToShortDateString(),
                e.Payee,
                e.Amount.ToString(CultureInfo.InvariantCulture),
                e.Description,
                e.Status,
                "X",
                "X",
                "[tag]"
            })).ToArray());
        }

        async void LoadFromReceiptBtn_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            ofd.ShowDialog();

            var fileInfo = new FileInfo(ofd.FileName);

            progressBar1.Value = 10;

            // http://localhost:7071/api/GetReceiptUploadSasToken

            // Get and use SAS token instead (or MI)
            await ConfigurationReader.GetAsync(StorageAccountConnectionString)
                                     .Map(cs => new BlobContainerClient(cs, "receipts"))
                                     .Map(client => client.GetBlobClient(fileInfo.Name))
                                     .Bind(client => client.UploadAsync(fileInfo.OpenRead(), true));

            progressBar1.Value += 20;

            var url =
                $"http://localhost:7071/api/GetReceiptData?blobName={fileInfo.Name}";

            var receipt =
                await _httpClient.GetStringAsync(url)
                                 .Map(json => JsonSerializer.Deserialize<Receipt>(json,
                                                                                        _jsonSerializerOptions));

            payeeTxt.Text = receipt.Payee;
            amountNud.Value = receipt.Amount;
            dateDtp.Value = receipt.Date;

            progressBar1.Value = 100;
        }

        async void AddBtn_Click(object sender, EventArgs e)
        {
            var expense = new Expense
            {
                Payee       = payeeTxt.Text,
                Description = descriptionTxt.Text,
                Date        = dateDtp.Value,
                Status      = statusCbx.SelectedText,
                Account     = accountCbx.SelectedText,
                Amount      = amountNud.Value,
                Id          = Guid.NewGuid(),
                Type        = typeCbx.SelectedText
            };

            var json = JsonSerializer.Serialize(expense);

            await _httpClient.PostAsync($"http://localhost:7071/api/{Constants.AddFunction}",
                                        new StringContent(json));
        }

        void RandomBtn_Click(object sender, EventArgs e)
        {
            var random = new Random(unchecked((int)DateTime.Now.Ticks));

            payeeTxt.Text = new [] { "Tesco", "Asda", "Sainsbury's", "Morrisons", "M&S", "Waitrose" }[random.Next(0, 6)];
            descriptionTxt.Text = Guid.NewGuid().ToString();
            dateDtp.Value = DateTime.Now.AddHours(random.Next(-24*30, -1));
            statusCbx.SelectedIndex = random.Next(0, 2);
            accountCbx.SelectedIndex = random.Next(0, 2);
            amountNud.Value = random.Next(0, 100) + random.Next(0, 99) * 0.01M;
            typeCbx.SelectedIndex = random.Next(0, 2);
        }
    }
}
