module UnoCash.Fulma.Pages

open UnoCash.Fulma.Models
open UnoCash.Fulma.ExpenseForm
open UnoCash.Fulma.ShowExpenses
open UnoCash.Fulma.About
open UnoCash.Fulma.Statistics

let page model =
    model |>
    match model.CurrentTab with
    | AddExpense     -> expenseFormCard "Add"
    | EditExpense    -> expenseFormCard "Edit"
    | ShowExpenses   -> showExpensesCard
    | About          -> aboutCard
    | ShowStatistics -> statisticsCard