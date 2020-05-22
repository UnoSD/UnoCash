module UnoCash.Fulma.Tabs

open Fulma
open Fable.React
open Fable.React.Props
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages

let private tab model dispatch tabType title =
    Tabs.tab [ Tabs.Tab.IsActive (model.CurrentTab = tabType) ]
             [ a [ OnClick (fun _ -> ChangeToTab tabType |> dispatch) ] [ str title ] ]

let tabs model dispatch =
    let tab tabType title =
        tab model dispatch tabType title
    
    Tabs.tabs [ Tabs.IsCentered ]
              [ tab AddExpense     "Add expense"
                tab ShowExpenses   "Show expenses" 
                tab ShowStatistics "Statistics"    
                tab About          "About"         ]