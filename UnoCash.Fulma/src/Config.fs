module UnoCash.Fulma.Config

open Fable.FontAwesome

[<Literal>]
let expenseSampleUrl = "http://localhost:7071/api/GetExpenses?account=Current"

let getExpensesUrl = "http://localhost:7071/api/GetExpenses"
let addExpenseUrl = "http://localhost:7071/api/AddExpense"
let deleteExpenseUrl = "http://localhost:7071/api/DeleteExpense"

let accounts = [ "Current"; "ISA"; "Wallet" ]

let initialAccount = "Current"

let tagIconLookup name =
    match name with
    | "groceries" -> Fa.Solid.ShoppingBag
    | "fuel"      -> Fa.Solid.Car
    | _           -> Fa.Solid.Tag