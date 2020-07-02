module UnoCash.Fulma.ShowExpenses

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages
open UnoCash.Fulma.Helpers
open Feliz
open System

let private tableHeader () =
    let allColumns =
        [
            "Date"
            "Payee"
            "Amount"
            "Status"
            "Type"
            "Tags"
            "Description"
        ]
        
    let columnsSubset =
        [
            "Date"
            "Payee"
            "Amount"
        ]
        
    let columns =
        (match isSmallScreen with
         | true  -> columnsSubset
         | false -> allColumns) |>
        List.map (fun columnName -> th [] [ str columnName ])
    
    let columnsWithActions =
        columns @ [ th [ Style [ Width "1%" ] ] [ str "Actions" ] ]
    
    thead []
          [ tr []
               columnsWithActions ]

let private row dispatch (expense : Expense) =
    let cellButton message icon =
        a [ onClick message dispatch
            Style [ PaddingLeft "10px"; PaddingRight "10px" ] ]
          [ Fa.i [ icon ] [] ]
          
    let deleteButton =
        cellButton (DeleteExpense expense.id) Fa.Solid.Trash
    
    let editButton =
        cellButton (EditExpense expense) Fa.Solid.PencilAlt
    
    let date =
        DateTime.Parse(expense.date).ToString("dd/MM/yy")
    
    let allCells =
        [
            date
            expense.payee
            expense.amount |> string
            expense.status
            expense.``type``
            expense.tags
            expense.description
        ]
        
    let cellsSubset =
        [
            date
            expense.payee
            expense.amount |> string
        ]
    
    let cells =
        (match isSmallScreen with
         | true  -> cellsSubset
         | false -> allCells) |>
        List.map (fun cellContent -> td [] [ str cellContent ])
    
    let cellsWithActions =
        cells @ [ td [ Style [ WhiteSpace WhiteSpaceOptions.Nowrap ] ]
                     [ deleteButton
                       editButton ] ]
    
    tr [] cellsWithActions

let private tableBody expenses dispatch =
    expenses |>
    Array.map (row dispatch) |>
    tbody []

let private expensesTable model dispatch =
    Table.table [ Table.IsBordered
                  Table.IsFullWidth
                  Table.IsStriped ]
                [ tableHeader ()
                  tableBody model.Expenses dispatch ]

let private getTotalByTypes types (expenses : Expense[]) =
    expenses |>
    Array.filter (fun x -> types |> List.contains x.``type``) |>
    Array.sumBy (fun x -> x.amount) |>
    decimal

let private totalLevelItem title total =
    Level.item [ Level.Item.HasTextCentered ]
               [ div []
                     [ Level.heading [] [ str title ]
                       Level.title   [] [ sprintf "£ %.2f" total |> str ] ] ]

let private totals expenses =
    [
        {|
            Types = [ "Regular" ]
            Title = "TOTAL RECONCILED"
        |}
        {|
            Types = [ "Regular"; "Pending" ]
            Title = "TOTAL"
        |}
    ] |>
    List.map (fun x -> expenses |> getTotalByTypes x.Types |> totalLevelItem x.Title) |>
    Level.level []

let private topBox model dispatch =
    Box.box' [] [ dropdownWithEvent "Account" model.Accounts model.ShowAccount ChangeShowAccount dispatch
                  totals model.Expenses ]

let showExpensesCard model dispatch =
    let getTable () =
        expensesTable model dispatch
    
    card [ topBox model dispatch
           spinnerOrContent model.ExpensesLoaded getTable ]
         Html.none