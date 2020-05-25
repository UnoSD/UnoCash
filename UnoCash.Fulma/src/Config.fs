module UnoCash.Fulma.Config

open Fable.FontAwesome

let getExpensesUrl = sprintf "%s/api/GetExpenses"
let addExpenseUrl = sprintf "%s/api/AddExpense"
let deleteExpenseUrl = sprintf "%s/api/DeleteExpense"
let receiptUploadSasTokenUrl = sprintf "%s/api/GetReceiptUploadSasToken"
let getReceiptDataUrl = sprintf "%s/api/GetReceiptData"
 
let storageAccount =
    "unocash"

let receiptStorageContainer =
    "receipts"

let accounts = [ "Current"; "ISA"; "Wallet" ]

let initialAccount = "Current"

let tagIconLookup name =
    match name with
    | "groceries" -> Fa.Solid.ShoppingBag
    | "fuel"      -> Fa.Solid.Car
    | _           -> Fa.Solid.Tag