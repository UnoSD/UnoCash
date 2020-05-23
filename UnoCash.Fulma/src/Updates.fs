module UnoCash.Fulma.Updates

open System
open Elmish
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages
open UnoCash.Fulma.Helpers
open UnoCash.Fulma.Config
open UnoCash.Fulma.Upload
open Fetch
open Fable.Core

let init _ =
    emptyModel, Cmd.none

let private loadExpenses account =
    fetch (sprintf "%s?account=%s" getExpensesUrl account) [] |>
    Promise.bind (fun x -> x.text()) |>
    Promise.map Expense.ParseArray

let private loadExpensesCmd account =
    Cmd.OfPromise.perform loadExpenses account ShowExpensesLoaded

let private addExpense model =
    let dateString =
        model.Date.ToString "O"
        
    let tagsCsv =
        model.Tags |> String.concat ","
        
    let newId =
        Guid.NewGuid() |> string
        
    let jsonComposer = sprintf """{
    "date": "%s",
    "payee": "%s",
    "amount": %f,
    "status": "%s",
    "type": "%s",
    "description": "%s",
    "account": "%s",
    "tags": "%s",
    "id": "%s"
}"""
   
    let body =
        jsonComposer dateString
                     model.Payee
                     model.Amount
                     model.Status
                     model.Type
                     model.Description
                     model.Account
                     tagsCsv
                     newId
    
    Fetch.fetch addExpenseUrl
                [ Method HttpMethod.POST
                  Body <| U3.Case3 body ]

let private removeExpense (id, account) =
    Fetch.fetch (sprintf "%s?id=%s&account=%s" deleteExpenseUrl id account)
                [ Method HttpMethod.DELETE ]
    
let private toModel (expense : Expense) =
    {
        Amount = decimal expense.amount
        Tags = expense.tags.Split(',') |> List.ofArray
        Date = DateTime.Parse expense.date
        Payee = expense.payee
        Account = expense.account
        Status = expense.status
        Type = expense.``type``
        Description = expense.description
    }

let private changeTabTo tab model =
    match tab with
    | ShowExpenses -> { model with CurrentTab = tab }, loadExpensesCmd model.ShowAccount
    | AddExpense   -> emptyModel, Cmd.none
    | _            -> { model with CurrentTab = tab }, Cmd.none

let private addTagOnEnter key tag model =
    match key with
    | "Enter" -> { model with Expense = { model.Expense with Tags = tag :: model.Expense.Tags |> List.distinct }
                              TagsText = String.Empty
                              Alert = match model.Expense.Tags |> List.exists ((=)tag) with
                                      | true  -> DuplicateTag
                                      | false -> NoAlert
                 }, Cmd.none
    | _       -> model, Cmd.none

let private withoutTag tagName expense =
    { expense with Tags = expense.Tags |> List.except [ tagName ] }

let addExpenseCmd expense =
    Cmd.OfPromise.perform addExpense expense (fun _ -> ChangeToTab AddExpense)

let removeExpenseCmd expId account =
    Cmd.OfPromise.perform removeExpense (expId, account) (fun _ -> ChangeToTab ShowExpenses)

let fileUploadCmd blob name length =
    Cmd.OfPromise.perform fileUpload (blob, name, length) (fun blobName -> ReceiptUploaded blobName)

let receiptParseCmd blobName =
    Cmd.OfPromise.perform receiptParse blobName (fun result -> ShowParsedExpense result)

let update message model =
    match message with
    | ChangeToTab newTab     -> changeTabTo newTab model
                                
    | TagsKeyDown (key, tag) -> addTagOnEnter key tag model
    
    | TagsTextChanged text   -> { model with TagsText = text }, Cmd.none
    
    | TagDelete tagName      -> { model with Expense = model.Expense |> withoutTag tagName }, Cmd.none
                             
    | ShowExpensesLoaded exs -> { model with Expenses = exs
                                             ExpensesLoaded = true }, Cmd.none
    
    | FileSelected fileName  -> { model with SelectedFile = match fileName with
                                                            | "" | null -> Option.None
                                                            | fileName  -> Some fileName }, Cmd.none
    | FileUpload (b, n, l)   -> { model with ReceiptAnalysis = { model.ReceiptAnalysis with Status = InProgress } },
                                fileUploadCmd b n l
    | ReceiptUploaded blob   -> model, receiptParseCmd blob
    | ShowParsedExpense exp  -> { model with Expense = exp
                                             ReceiptAnalysis = { model.ReceiptAnalysis with Status = Completed } },
                                Cmd.none
    
    | AddNewExpense          -> emptyModel, addExpenseCmd model.Expense
                             
    | ChangePayee text       -> { model with Expense = { model.Expense with Payee = text } }, Cmd.none
    | ChangeDate newDate     -> { model with Expense = { model.Expense with Date = DateTime.Parse(newDate) } },
                                Cmd.none
    | ChangeAmount newValue  -> { model with Expense = { model.Expense with Amount = toDecimal newValue 2 } },
                                Cmd.none
    | ChangeAccount text     -> { model with Expense = { model.Expense with Account = text } }, Cmd.none
    | ChangeStatus text      -> { model with Expense = { model.Expense with Status = text } }, Cmd.none
    | ChangeType text        -> { model with Expense = { model.Expense with Type = text } }, Cmd.none
    | ChangeDescription txt  -> { model with Expense = { model.Expense with Description = txt } }, Cmd.none
                             
    | ChangeShowAccount acc  -> match model.ShowAccount = acc with
                                | true  -> model, Cmd.none
                                | false -> { model with ShowAccount = acc }, loadExpensesCmd acc
                                
    | DeleteExpense expId    -> model, removeExpenseCmd expId model.ShowAccount
    | EditExpense expense    -> { model with Expense = expense |> toModel
                                             CurrentTab = Tab.EditExpense }, Cmd.none