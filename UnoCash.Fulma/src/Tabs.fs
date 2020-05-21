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
    Tabs.tabs [ Tabs.IsCentered ]
              [ tab model dispatch AddExpense     "Add expense"
                tab model dispatch ShowExpenses   "Show expenses" 
                tab model dispatch ShowStatistics "Statistics"    
                tab model dispatch About          "About"         ]