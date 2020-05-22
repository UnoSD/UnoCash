module UnoCash.Fulma.Pages

open Fable.React
open UnoCash.Fulma.Models
open UnoCash.Fulma.ExpenseForm
open UnoCash.Fulma.ShowExpenses
open UnoCash.Fulma.About
open Feliz

let page model =
    model |>
    match model.CurrentTab with
    | AddExpense   -> expenseFormCard "Add"
    | EditExpense  -> expenseFormCard "Edit"
    | ShowExpenses -> showExpensesCard
    | About        -> aboutCard
    | _            -> (fun _ _ -> Html.div [ str "Not implemented" ])