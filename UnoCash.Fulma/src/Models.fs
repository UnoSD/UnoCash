module UnoCash.Fulma.Models

open Fable
open System
open Microsoft.FSharp.Reflection
open UnoCash.Fulma.Helpers
open UnoCash.Fulma.Config

type Expense =
    JsonProvider.Generator<expenseSampleUrl>

type Tab =
    | AddExpense
    | EditExpense
    | ShowExpenses
    | ShowStatistics
    | About

type AlertType =
    | NoAlert
    | DuplicateTag

type Receipt =
    {
        Payee : string option
        Date : DateTime option
        Method : string option
        Amount : float option
    }

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

type ReceiptAnalysisStatus =
    | NotStarted
    | InProgress
    | Completed

type ReceiptAnalysis =
    {
        Status : ReceiptAnalysisStatus
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
        Accounts : string list
        ReceiptAnalysis : ReceiptAnalysis
    }
    
let emptyModel = 
    {
        CurrentTab = AddExpense
        TagsText = ""
        Alert = NoAlert
        Expenses = [||]
        SelectedFile = Option.None
        ExpensesLoaded = false
        ShowAccount = initialAccount
        SelectedExpenseId = String.Empty
        Accounts = accounts
        ReceiptAnalysis = { Status = NotStarted }
        Expense =
        {
            Date = DateTime.Today
            Tags = []
            Amount = 0m
            Payee = String.Empty
            Account = initialAccount
            Status = "New"
            Type = "Regular"
            Description = String.Empty
        }
    }

type ExpenseType =
    | New
    | Pending
    | Scheduled

type ExpenseStatus =
    | Regular
    | InternalTransfer
    | Scheduled

let inline enumerateCases<'a> =
    FSharpType.GetUnionCases typeof<'a> |>
    Array.map (fun case -> pascalCaseToDisplay case.Name)

let expenseTypes =
    enumerateCases<ExpenseType>
    
let expenseStatus =
    enumerateCases<ExpenseStatus>