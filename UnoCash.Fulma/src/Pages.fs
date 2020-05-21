module UnoCash.Fulma.Pages

open Fable.React
open Fable.React.Props
open Fulma
open Fable.FontAwesome
open UnoCash.Fulma.Models
open UnoCash.Fulma.Messages

let private receiptUpload model dispatch =
    let filename =
        match model.SelectedFile with
        | Some f      -> f
        | Option.None -> "No receipt selected" 
    Field.div [ ]
              [ File.file [ File.HasName ]
                          [ File.label [ ]
                                       [ File.input [ Props [ OnChange (fun ev -> FileSelected ev.Value |> dispatch) ] ]
                                         File.cta [ ]
                                                  [ File.icon [ ]
                                                              [ Icon.icon [ ]
                                                                          [ Fa.i [ Fa.Solid.Upload ] [ ] ] ]
                                                    File.label [ ] [ str "Upload a receipt..." ] ]
                                         File.name [ ] [ str filename ] ] ] ]

let onDdChange msg dispatch =
    OnChange (fun ev -> msg ev.Value |> dispatch)

let private dropdown title items prop value =
    let options items =
        items |>
        List.map (fun item -> option [ Value item ] [ str item ])

    Field.div [ ]
              [ Label.label [ ] [ str title ]
                Control.div [ ]
                            [ Select.select [ ]
                                            [ select [ Value value; prop ]
                                                     (options items) ] ] ]

let private tags model dispatch =
    let tag name =
        let iconLookup name =
            match name with
            | "groceries" -> Fa.Solid.ShoppingBag
            | "fuel"      -> Fa.Solid.Car
            | _           -> Fa.Solid.Tag
        Control.div [ ]
                    [ Tag.list [ Tag.List.HasAddons ]
                               [ Tag.tag [ Tag.Color IsInfo ] [ Icon.icon [ ] [ Fa.i [ iconLookup name ] [ ] ] ]
                                 Tag.tag [ Tag.Color IsLight ] [ str name ]
                                 Tag.delete [ Tag.Props [ OnClick (fun _ -> TagDelete name |> dispatch) ] ] [ ] ] ]

    model.Tags |>
    List.map tag |>
    Field.div [ Field.IsGroupedMultiline ]

let private ifDuplicateTagAlertAdd element model =
    match model.Alert with
    | DuplicateTag -> [ element ]
    | _            -> []

let onChange msg dispatch =
    Input.OnChange (fun ev -> msg ev.Value |> dispatch)
    
let onTaChange msg dispatch =
    Textarea.OnChange (fun ev -> msg ev.Value |> dispatch)

let private expenseForm model dispatch =
    form [ ]
         [ receiptUpload model dispatch
                    
           Field.div [ ]
                     [ Label.label [ ] [ str "Payee" ]
                       Control.div [ Control.HasIconLeft ]
                                   [ Input.text [ Input.Placeholder "Ex: Tesco"
                                                  Input.Props [ AutoFocus true ]
                                                  Input.Value model.Expense.Payee
                                                  onChange ChangePayee dispatch ]
                                     Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ]
                                               [ Fa.i [ Fa.Solid.CashRegister ] [ ] ] ] ]

           Field.div [ ]
                [ Label.label [ ] [ str "Date" ]
                  Control.div [ Control.HasIconLeft ]
                              [ Input.date [ Input.Value (model.Expense.Date.ToString("yyyy-MM-dd"))
                                             onChange DateChanged dispatch ]
                                Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ]
                                          [ Fa.i [ Fa.Solid.CalendarDay ] [ ] ] ] ]

           Field.div [ ]
                     [ Label.label [ ] [ str "Amount" ]
                       Control.div [ Control.HasIconLeft ]
                                   [ Input.number [ Input.Props [ Props.Step "0.01" ]
                                                    onChange ChangeAmount dispatch
                                                    Input.Value (string model.Expense.Amount) ]
                                     Icon.icon [ Icon.Size IsSmall
                                                 Icon.IsLeft ]
                                               [ Fa.i [ Fa.Solid.DollarSign ] [ ] ] ] ]
 
           div [ Style [ Display DisplayOptions.InlineFlex ] ]
               [ dropdown "Account" [ "Current"; "ISA"; "Wallet" ] (onDdChange ChangeAccount dispatch) model.Expense.Account

                 dropdown "Status" [ "New"; "Pending"; "Reconciled" ] (onDdChange ChangeStatus dispatch) model.Expense.Status

                 dropdown "Type" [ "Regular"; "Internal transfer"; "Scheduled" ] (onDdChange ChangeType dispatch) model.Expense.Type ]

           Field.div [ ]
                     ([ Label.label [ ] [ str "Tags" ]
                        Control.div [ Control.HasIconLeft; Control.HasIconRight ]
                                    ([ Input.text ([ Input.Placeholder "Ex: groceries"
                                                     Input.Value model.TagsText
                                                     Input.OnChange (fun ev -> TagsTextChanged ev.Value |> dispatch)
                                                     Input.Props [ OnKeyDown (fun ev -> TagsKeyDown (ev.key, ev.Value) |> dispatch) ] ] @
                                                     ifDuplicateTagAlertAdd (Input.Color IsDanger) model)
                                       Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.i [ Fa.Solid.Tags ] [ ] ] ] @
                                       (ifDuplicateTagAlertAdd (Icon.icon [ Icon.Size IsSmall; Icon.IsRight ] [ Fa.i [ Fa.Solid.ExclamationTriangle ] [ ] ]) model)) ] @ 
                        (ifDuplicateTagAlertAdd (Help.help [ Help.Color IsDanger ] [ str "Duplicate tag" ]) model))
           
           tags model.Expense dispatch

           Field.div [ ]
                     [ Label.label [ ] [ str "Description" ]
                       Control.div [ Control.IsLoading true ]
                                   [ Textarea.textarea [ onTaChange ChangeDescription dispatch; Textarea.Option.Value model.Expense.Description ] [ ] ] ] ]

let private expensesRows model dispatch =
    let row (expense : Expense) =
        tr ( match expense.id = model.SelectedExpenseId with | false -> [] | true -> [ ClassName "is-selected" ])
           [ td [ ] [ str expense.date ]
             td [ ] [ str expense.payee ]
             td [ ] [ str (string expense.amount) ]
             td [ ] [ str expense.status ]
             td [ ] [ str expense.``type`` ]
             td [ ] [ str expense.tags ]
             td [ ] [ str expense.description ]
             td [ Style [ WhiteSpace WhiteSpaceOptions.Nowrap ] ]
                [ a [ OnClick (fun _ -> DeleteExpense expense.id |> dispatch); Style [ PaddingLeft "10px"; PaddingRight "10px" ] ]
                    [ Fa.i [ Fa.Solid.Trash ] [] ]
                  a [ OnClick (fun _ -> EditExpense expense |> dispatch); Style [ PaddingLeft "10px"; PaddingRight "10px" ] ]
                    [ Fa.i [ Fa.Solid.PencilAlt ] [] ] ] ]
           
    model.Expenses |>
    Array.map row
    
let private expensesTable model dispatch =
    let table model dispatch =
        Table.table [ Table.IsBordered
                      Table.IsFullWidth
                      Table.IsStriped ]
                    [ thead [ ]
                            [ tr [ ]
                                 [ th [ ] [ str "Date" ]
                                   th [ ] [ str "Payee" ]
                                   th [ ] [ str "Amount" ]
                                   th [ ] [ str "Status" ]
                                   th [ ] [ str "Type" ]
                                   th [ ] [ str "Tags" ]
                                   th [ ] [ str "Description" ]
                                   th [ Style [ Width "1%" ] ] [ str "Actions" ] ] ]
                      tbody [ ] (expensesRows model dispatch) ]
    
    match model.ExpensesLoaded with
    | true  -> table model dispatch
    | false -> div [ Class ("block " + Fa.Classes.Size.Fa3x)
                     Style [ TextAlign TextAlignOptions.Center ] ]
                   [ Fa.i [ Fa.Solid.Sync; Fa.Spin ] [  ] ]

let private card content footer =
    Card.card [ ]
              ([ Card.content [ ]
                              [ Content.content [ ] content ] ] @ footer)

let private showExpensesCard model dispatch =
    card [ dropdown "Account" [ "Current"; "ISA"; "Wallet" ] (onDdChange ChangeShowAccount dispatch) model.ShowAccount
           expensesTable model dispatch ] []

let private buttons dispatch submitText =
    Card.footer [ ]
                [ Card.Footer.a [ Props [ OnClick (fun _ -> AddNewExpense |> dispatch) ] ]
                                [ str submitText ]
                  Card.Footer.a [ ]
                                [ str "Split" ] ]
                
let expenseFormCard submitButtonText model dispatch =
    card [ expenseForm model dispatch ]
         [ buttons dispatch submitButtonText ]

let page model =
    model |>
    match model.CurrentTab with
    | AddExpense      -> expenseFormCard "Add"
    | Tab.EditExpense -> expenseFormCard "Edit"
    | ShowExpenses    -> showExpensesCard
    | _               -> (fun _ _ -> Helpers.div [ str "Not implemented" ])