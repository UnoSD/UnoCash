module UnoCash.Fulma.Updates

open System
open Elmish
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages
open Fetch

let init _ =
    emptyModel, Cmd.none

let private sanitize value =
    match Decimal.TryParse(value) with
    | true, dec -> (dec * 100m |> Decimal.Truncate) / 100m
    | false, _  -> 0m

let loadExpenses account =
    fetch (sprintf "http://localhost:7071/api/GetExpenses?account=%s" account) [] |>
    Promise.bind (fun x -> x.text()) |>
    Promise.map Expense.ParseArray

let private loadExpensesCmd account =
    Cmd.OfPromise.perform loadExpenses
                          account
                          ShowExpensesLoaded

let private addExpense model =
    Fetch.fetch "http://localhost:7071/api/AddExpense"
                [ Method HttpMethod.POST
                  Body <| Fable.Core.U3.Case3 (sprintf """{
    "date": "%A",
    "payee": "%A",
    "amount": %A,
    "status": "%A",
    "type": "%A",
    "description": "%A",
    "account": "%A",
    "tags": "%s",
    "id": "%A"
}"""                                    (model.Date.ToString("O"))
                                        model.Payee
                                        model.Amount
                                        model.Status
                                        model.Type
                                        model.Description
                                        model.Account
                                        (model.Tags |> String.concat ",")
                                        (Guid.NewGuid().ToString())) ]

let private removeExpense (id, account) =
    Fetch.fetch (sprintf "http://localhost:7071/api/DeleteExpense?id=%s&account=%s" id account)
                [ Method HttpMethod.DELETE ]
    
let private toModel (expense : Expense) =
    {
        Amount = decimal expense.amount
        Tags = expense.tags.Split(",".[0]) |> List.ofArray
        Date = DateTime.Parse expense.date
        Payee = expense.payee
        Account = expense.account
        Status = expense.status
        Type = expense.``type``
        Description = expense.description
    }

let update msg model =
    match msg with
    | ChangeToTab newTab    -> match newTab with
                               | ShowExpenses -> { model with CurrentTab = newTab }, loadExpensesCmd model.ShowAccount
                               | AddExpense   -> emptyModel, Cmd.none
                               | _            -> { model with CurrentTab = newTab }, Cmd.none
    | ChangeAmount newValue -> { model with Expense = { model.Expense with Amount = sanitize newValue } }, Cmd.none
    | TagsKeyDown (key, x)  -> match key with
                               | "Enter" -> {
                                                model with Expense = { model.Expense with Tags = x :: model.Expense.Tags |> List.distinct }
                                                           TagsText = String.Empty
                                                           Alert = match model.Expense.Tags |> List.exists ((=)x) with
                                                                   | true  -> DuplicateTag
                                                                   | false -> None
                                            }, Cmd.none
                               | _       -> model, Cmd.none
    | TagsTextChanged text  -> { model with TagsText = text }, Cmd.none
    | TagDelete tagName     -> { model with Expense = { model.Expense with Tags = model.Expense.Tags |> List.except [ tagName ] } }, Cmd.none
    | DateChanged newDate   -> { model with Expense = { model.Expense with Date = DateTime.Parse(newDate) } }, Cmd.none
    | ShowExpensesLoaded es -> { model with Expenses = es; ExpensesLoaded = true }, Cmd.none
    | FileSelected fileName -> { model with SelectedFile = match fileName with
                                                           | "" | null -> Option.None
                                                           | x -> Some x }, Cmd.none
    | AddNewExpense         -> emptyModel, Cmd.OfPromise.perform addExpense model.Expense (fun _ -> ChangeToTab AddExpense)
    | ChangePayee text      -> { model with Expense = { model.Expense with Payee = text } }, Cmd.none
    | ChangeAccount text    -> { model with Expense = { model.Expense with Account = text } }, Cmd.none
    | ChangeStatus text     -> { model with Expense = { model.Expense with Status = text } }, Cmd.none
    | ChangeType text       -> { model with Expense = { model.Expense with Type = text } }, Cmd.none
    | ChangeDescription txt -> { model with Expense = { model.Expense with Description = txt } }, Cmd.none
    | ChangeShowAccount acc -> match model.ShowAccount = acc with
                               | true  -> model, Cmd.none
                               | false -> { model with ShowAccount = acc }, loadExpensesCmd acc
    | DeleteExpense expId   -> model, Cmd.OfPromise.perform removeExpense (expId, model.ShowAccount) (fun _ -> ChangeToTab ShowExpenses)
    | EditExpense expense   -> { model with Expense = expense |> toModel; CurrentTab = Tab.EditExpense }, Cmd.none