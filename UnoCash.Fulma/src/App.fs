module App.View

open Elmish
open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open Fulma.Elmish
open System

type Tab =
    | AddExpense
    | ShowExpenses
    | ShowStatistics
    | About

type Model =
    { 
        Value : string
        CurrentTab : Tab
        DatePickerState : DatePicker.Types.State
        CurrentDate : DateTime option
        Amount : decimal
    }

type Msg =
    | ChangeValue of string
    | ChangeToTab of Tab
    | DatePickerChanged of DatePicker.Types.State * (DateTime option)
    | AmountChanged of string

let init _ =
    {
        Value = ""
        CurrentTab = AddExpense
        DatePickerState = { DatePicker.Types.defaultState with AutoClose = true; ShowDeleteButton = true }
        CurrentDate = None
        Amount = 0m
    }, Cmd.none

let sanitize value =
    match Decimal.TryParse(value) with
    | true, dec -> (dec * 100m |> Decimal.Truncate) / 100m
    | false, _  -> 0m

let private update msg model =
    match msg with
    | ChangeValue newValue ->
        { model with Value = newValue }, Cmd.none
    | ChangeToTab newTab ->
        { model with CurrentTab = newTab }, Cmd.none
    | DatePickerChanged (newState, date) ->
        { model with DatePickerState = newState
                     CurrentDate = date }, Cmd.none
    | AmountChanged newValue -> { model with Amount = sanitize newValue }, Cmd.none

let pickerConfig : DatePicker.Types.Config<Msg> =
    DatePicker.Types.defaultConfig DatePickerChanged

let private view model dispatch =
    Hero.hero [ Hero.IsFullHeight ]
        [ Hero.body [ ]
            [ Container.container [ ]
                [ Columns.columns [ Columns.CustomClass "has-text-centered" ]
                    [ Column.column [ Column.Width(Screen.All, Column.IsOneThird)
                                      Column.Offset(Screen.All, Column.IsOneThird) ]
                        [ Image.image [ Image.Is128x128
                                        Image.Props [ Style [ Margin "auto"] ] ]
                            [ img [ Src "assets/fulma_logo.svg" ] ]
                          Field.div [ ]
                            [ Label.label [ ]
                                [ str "Enter your name, dumbass!!!!" ]
                              Control.div [ ]
                                [ Input.text [ Input.OnChange (fun ev -> dispatch (ChangeValue ev.Value))
                                               Input.Value model.Value
                                               Input.Props [ AutoFocus true ] ] ] ]
                          Content.content [ ]
                            [ str "Hello, "
                              str model.Value
                              str " "
                              Icon.icon [ ]
                                [ Fa.i [ Fa.Regular.Smile ]
                                    [ ] ] ] ] ] ] ] ]

let tab model dispatch tabType title =
    Tabs.tab [ Tabs.Tab.IsActive (model.CurrentTab = tabType) ]
             [ a [ OnClick (fun _ -> ChangeToTab tabType |> dispatch) ] [ str title ] ]

//Field.div [ ]
//          [ Label.label [ ] [ str "Date" ]
//            Control.div [ ] [ DatePicker.View.root pickerConfig model.DatePickerState model.CurrentDate dispatch ] ]

let tags =
    Field.div [ Field.IsGroupedMultiline ]
              [ Control.div [ ]
                  [ Tag.list [ Tag.List.HasAddons ]
                      [ Tag.tag [ Tag.Color IsDanger ] [ str "Maxime Mangel" ]
                        Tag.delete [ ] [ ] ] ]
                Control.div [ ]
                  [ Tag.list [ Tag.List.HasAddons ]
                      [ Tag.tag [ Tag.Color IsLight ] [ str "28 Februari 1982" ]
                        Tag.tag [ Tag.Color IsInfo ]
                          [ Icon.icon [ ] [ i [ ClassName "fas fa-birthday-cake" ] [ ] ] ] ] ]
                Control.div [ ]
                  [ Tag.list [ Tag.List.HasAddons ]
                      [ Tag.tag [ Tag.Color IsLight ] [ str "37 years"  ]
                        Tag.tag [ Tag.Color IsInfo ] [ str "age" ] ] ] ]

let addExpensePage model dispatch =
    form [ ]
         [ Field.div [ ]
                 [ File.file [ File.HasName ]
                     [ File.label [ ]
                         [ File.input [ ]
                           File.cta [ ]
                             [ File.icon [ ]
                                 [ Icon.icon [ ]
                                     [ Fa.i [ Fa.Solid.Upload ]
                                         [ ] ] ]
                               File.label [ ]
                                 [ str "Upload a receipt..." ] ]
                           File.name [ ]
                             [ str "No receipt selected" ] ] ] ]
           
           Field.div [ ]
                [ Label.label [ ] [ str "Date" ]
                  Control.div [ Control.HasIconLeft ]
                              [ Input.date [ Input.DefaultValue (DateTime.Today |> string) ]
                                Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ]
                                          [ Fa.i [ Fa.Solid.CalendarDay ] [ ] ] ] ]
                    
           Field.div [ ]
                     [ Label.label [ ] [ str "Payee" ]
                       Control.div [ Control.HasIconLeft ]
                                   [ Input.text [ Input.Placeholder "Ex: Tesco" ]
                                     Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ]
                                               [ Fa.i [ Fa.Solid.CashRegister ] [ ] ] ] ]

           Field.div [ ]
                     [ Label.label [ ] [ str "Amount" ]
                       Control.div [ Control.HasIconLeft ]
                                   [ Input.number [ Input.DefaultValue "0.00"
                                                    Input.Props [ Props.Step "0.01" ]
                                                    Input.OnChange (fun ev -> AmountChanged ev.Value |> dispatch)
                                                    Input.Value (string model.Amount) ]
                                     Icon.icon [ Icon.Size IsSmall
                                                 Icon.IsLeft ]
                                               [ Fa.i [ Fa.Solid.DollarSign ] [ ] ] ] ]
 
           // Without Control.HasIconLeft fits in a phone screen
           div [ Style [ Display DisplayOptions.InlineFlex ] ] [ 
                        Field.div [ ]
                                  [ Label.label [ ] [ str "Account" ]
                                    Control.div [ Control.HasIconLeft ]
                                                [ Select.select [ ]
                                                                [ select [ DefaultValue "1" ]
                                                                         [ option [ Value "1"] [ str "Current" ]
                                                                           option [ Value "2"] [ str "ISA" ]
                                                                           option [ Value "3"] [ str "Wallet" ] ] ] ] ]

                        Field.div [ ]
                                  [ Label.label [ ] [ str "Status" ]
                                    Control.div [ Control.HasIconLeft ]
                                                [ Select.select [ ]
                                                                [ select [ DefaultValue "1" ]
                                                                         [ option [ Value "1"] [ str "Current" ]
                                                                           option [ Value "2"] [ str "ISA" ]
                                                                           option [ Value "3"] [ str "Wallet" ] ] ] ] ]

                        Field.div [ ]
                                  [ Label.label [ ] [ str "Type" ]
                                    Control.div [ Control.HasIconLeft ]
                                                [ Select.select [ ]
                                                                [ select [ DefaultValue "1" ]
                                                                         [ option [ Value "1"] [ str "" ]
                                                                           option [ Value "2"] [ str "" ]
                                                                           option [ Value "3"] [ str "Scheduled" ] ] ] ] ]
           ]

           Field.div [ ]
                     [ Label.label [ ] [ str "Tags" ]
                       Control.div [ Control.HasIconLeft ]
                                   [ Input.text [ Input.Placeholder "Ex: groceries" ]
                                     Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.i [ Fa.Solid.Tags ] [ ] ] ] ]
           

           Field.div [ Field.IsGroupedMultiline ]
                     [ Control.div [ ]
                                   [ Tag.list [ Tag.List.HasAddons ]
                                              [ Tag.tag [ Tag.Color IsInfo ] [ Icon.icon [ ] [ Fa.i [ Fa.Solid.ShoppingBag ] [ ] ] ]
                                                Tag.tag [ Tag.Color IsLight ] [ str "groceries" ]
                                                Tag.delete [ ] [ ] ] ]
                       Control.div [ ]
                                   [ Tag.list [ Tag.List.HasAddons ]
                                              [ Tag.tag [ Tag.Color IsInfo ] [ Icon.icon [ ] [ Fa.i [ Fa.Solid.Car ] [ ] ] ]
                                                Tag.tag [ Tag.Color IsLight ] [ str "fuel" ]
                                                Tag.delete [ ] [ ] ] ] ]

           Field.div [ ]
                     [ Label.label [ ] [ str "Description" ]
                       Control.div [ Control.IsLoading true ]
                                   [ Textarea.textarea [ ] [ ] ] ] ]

let page model dispatch =
    match model.CurrentTab with
    | AddExpense   -> addExpensePage model dispatch
    | ShowExpenses
    | _ -> div [ ] [ str "Not implemented" ]

let private view' model dispatch =
    div [ ] [ Tabs.tabs [ Tabs.IsCentered ]
                        [ tab model dispatch AddExpense     "Add expense"
                          tab model dispatch ShowExpenses   "Show expenses" 
                          tab model dispatch ShowStatistics "Statistics"    
                          tab model dispatch About          "About"         ]
              Card.card [ ]
                        [ Card.content [ ]
                                       [ Content.content [ ]
                                                         [ page model dispatch ] ]
                          Card.footer [ ]
                                      [ Card.Footer.a [ ]
                                                      [ str "Add" ]
                                        Card.Footer.a [ ]
                                                      [ str "Split" ] ] ] ]

open Elmish.Debug
open Elmish.HMR

Program.mkProgram init update view'
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
