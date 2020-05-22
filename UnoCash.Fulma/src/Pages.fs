module UnoCash.Fulma.Pages

open Fable.React
open UnoCash.Fulma.Models
open UnoCash.Fulma.ExpenseForm
open UnoCash.Fulma.ShowExpenses
open Feliz

let page model =
    model |>
    match model.CurrentTab with
    | AddExpense      -> expenseFormCard "Add"
    | Tab.EditExpense -> expenseFormCard "Edit"
    | ShowExpenses    -> showExpensesCard
    | _               -> (fun _ _ -> Html.div [ str "Not implemented" ])