module UnoCash.Fulma.Messages

open UnoCash.Fulma.Models

type Msg =
    | ChangeToTab of Tab
    | ChangeAmount of string
    | ChangePayee of string
    | TagsKeyDown of string * string
    | TagsTextChanged of string
    | TagDelete of string
    | DateChanged of string
    | ShowExpensesLoaded of Expense[]
    | FileSelected of string
    | AddNewExpense
    | ChangeAccount of string
    | ChangeStatus of string
    | ChangeType of string
    | ChangeShowAccount of string
    | ChangeDescription of string
    | DeleteExpense of string
    | EditExpense of Expense