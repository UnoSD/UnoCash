using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using UnoCash.Core;
using UnoCash.Dto;
using UnoCash.Shared;

namespace UnoCash.Blazor.Pages
{
    public partial class Update
    {
        protected bool IsModalOpen;

        [Inject] protected IJSRuntime Js { get; set; }

        [Inject] protected HttpClient Http { get; set; }

        protected int AnalysisProgress { get; set; }

        [Parameter]
        public string Id // Use partial class
        {
            get => Guid.ToString();
            set => Guid = Guid.Parse(value);
        }

        protected Task UploadToBlobStorage()
        {
            AnalysisProgress = 25;

            return UploadToBlobStorageJs(DotNetObjectReference.Create(this)).AsTask();
        }

        ValueTask<object> UploadToBlobStorageJs(DotNetObjectReference<Update> instance) =>
            Js.InvokeAsync<object>("uploadToBlobStorage",
                                   instance,
                                   "receipts", // Get from config // Create if it doesn't exist
                                   nameof(OnBlobUploaded),
                                   nameof(GetSasToken),
                                   // Get the whole upload URL including token from an endpoint
                                   "https://unocash.blob.core.windows.net");
        // Environment.GetEnvironmentVariable("UNOCASH_BLOB_URI"));
        // https://STORAGEACCOUNTNAME.blob.core.windows.net
        // http://127.0.0.1:10000/devstoreaccount1

        [JSInvokable]
        public Task<string> GetSasToken()
        {
            AnalysisProgress = 40;

            return Http.GetStringAsync("http://localhost:7071/api/GetReceiptUploadSasToken");
        }

        [JSInvokable]
        public async Task OnBlobUploaded(string blobName)
        {
            AnalysisProgress = 75;
            StateHasChanged();

            var url =
                $"http://localhost:7071/api/GetReceiptData?blobName={blobName}";

            var receipt =
                await Http.GetJsonAsync<Receipt>(url);

            Expense.Payee    = receipt.Payee;
            AmountBinder     = receipt.Amount.ToString(CultureInfo.InvariantCulture);
            DateBinder       = receipt.Date.ToString(CultureInfo.InvariantCulture);
            AnalysisProgress = 100;

            StateHasChanged();
        }

        protected string GetTitle() =>
            Guid == Guid.Empty ? "Add an expense" : $"Edit the expense {Id}";

        protected Guid Guid { get; set; }

        // If I remove the new Expense, I get a null exception,
        // probably OnInitAsync happens after the binding
        // Do that BEFORE the binding
        protected Expense Expense = new Expense();
        List<string> _tags = new List<string>();

        string _searchText = string.Empty;
        string _searchDesc = string.Empty;
        string _popOverColor = "";
        bool _popOver = false;

        Dictionary<string, string> _tagsDb = new Dictionary<string, string>
        {
            ["pippo"] = "Disney character",
            ["peppe"] = "Neapolitan name"
        };
        
        void HandleKeyPress(KeyboardEventArgs args)
        {
            if (args.Code != "Enter")
            {
                _searchText += args.Key;
                
                _searchDesc   = _tagsDb.TryGetValue(_searchText, out var desc) ? desc : "New tag";
                _popOverColor = _searchDesc == "New tag" ? "" : ".red";
            
                _popOver = true;
                StateHasChanged();
                return;
            }

            _popOver = false;
            StateHasChanged();
            
            _tags.Add(_searchText); // If not already
            _searchText = string.Empty;
        }

    protected override async Task OnInitializedAsync() =>
            Expense =
                Guid == Guid.Empty ?
                    new Expense
                    {
                        Date = DateTime.Today,
                        Amount = 99,
                        Account = "Current",
                        Type = "Regular",
                        Status = "New",
                        Description = ""
                    } :
                (await Http.GetJsonAsync<Expense[]>(GetExpensesUrl(Id))).Single();

        static string GetExpensesUrl(string id) => 
            $"http://localhost:7071/api/GetExpenses?account=Current&id={id}";

        protected string AmountBinder
        {
            get => Expense.Amount.ToString(CultureInfo.InvariantCulture);
            set => Expense.Amount = decimal.TryParse(value, out var result) ? result : 0;
        }

        public string DateBinder
        {
            get => Expense.Date.ToString("yyyy-MM-dd");
            set => Expense.Date = DateTime.TryParse(value, out var result) ? result : DateTime.Today;
        }

        static HttpRequestMessage CreatePatchRequest(Expense expense) =>
            new HttpRequestMessage(new HttpMethod("PATCH"), "http://localhost:7071/api/UpdateExpense")
            {
                Content = new StringContent(JsonSerializer.Serialize(expense),
                                            Encoding.UTF8,
                                            "application/json")
            };

        public Task AddOrUpdate()
        {
            Expense.Id = Guid;

            return Guid == Guid.Empty ?
                   Http.PostJsonAsync($"http://localhost:7071/api/{Constants.AddFunction}", Expense) :
                   CreatePatchRequest(Expense).UsingAsync(Http.SendAsync);
        }
    }
}
