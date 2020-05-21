module UnoCash.Fulma.Models

open Fable
open System

type Expense =
    JsonProvider.Generator<"http://localhost:7071/api/GetExpenses?account=Current">

type Tab =
    | AddExpense
    | ShowExpenses
    | ShowStatistics
    | About
    | EditExpense

type AlertType =
    | None
    | DuplicateTag

type ExpenseModel =
    {
        Amount : decimal
        Tags : string list
        Date : DateTime
        Payee : string
        Account : string
        Status : string
        Type : string
        Description : string
    }

type Model =
    {
        CurrentTab : Tab
        TagsText : string
        Alert : AlertType
        Expenses : Expense[]
        SelectedFile : string option
        ExpensesLoaded : bool
        Expense : ExpenseModel
        ShowAccount : string
        SelectedExpenseId : string
    }
    
let emptyModel = 
    {
        CurrentTab = AddExpense
        TagsText = ""
        Alert = None
        Expenses = [||]
        SelectedFile = Option.None
        ExpensesLoaded = false
        ShowAccount = "Current"
        SelectedExpenseId = ""
        Expense =
        {
            Date = DateTime.Today
            Tags = []
            Amount = 0m
            Payee = ""
            Account = "Current"
            Status = "New"
            Type = "Regular"
            Description = ""
        }
    }