module UnoCash.Fulma.ExpenseForm

open Fable.FontAwesome
open Fable.React
open Fable.React.Props
open Feliz
open Fulma
open UnoCash.Fulma.Messages
open UnoCash.Fulma.Helpers
open UnoCash.Fulma.Models
open UnoCash.Fulma.Config

let private buttons dispatch submitText =
    Card.footer []
                [ Card.Footer.a [ Props [ onClick AddNewExpense dispatch ] ]
                                [ str submitText ]
                  Card.Footer.a [ ]
                                [ str "Split" ] ]

let private expenseForm model dispatch =
    let dropdown title options value msg =
        dropdownWithEvent title options value msg dispatch
    
    let inlineElements elements =
        div [ Style [ Display DisplayOptions.InlineFlex ] ]
            elements
    
    let descriptionField =
        Field.div [ ]
                  [ Label.label [ ] [ str "Description" ]
                    Control.div [ Control.IsLoading true ]
                                [ Textarea.textarea [ Textarea.Option.Value model.Expense.Description
                                                      Textarea.Props [ onChange ChangeDescription dispatch ] ] [] ] ]
    
    let simpleField labelText icon input =
        Field.div []
                  [ Label.label [] [ str labelText ]
                    Control.div [ Control.HasIconLeft ]
                                [ input
                                  Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ]
                                            [ Fa.i [ icon ] [ ] ] ] ]
    
    let amountField =
        Input.number [ Input.Props [ Props.Step "0.01"
                                     onChange ChangeAmount dispatch ]
                       Input.Value (string model.Expense.Amount) ] |>
        simpleField "Amount" Fa.Solid.DollarSign
    
    let addEmptyOnAlert empty element =
        match model.Alert with
        | DuplicateTag -> element
        | _            -> empty
    
    let addOnAlert element =
        addEmptyOnAlert Html.none element
    
    let tagsInputIcon =
        Icon.icon [ Icon.Size IsSmall; Icon.IsLeft ] [ Fa.i [ Fa.Solid.Tags ] [ ] ]
    
    let tagsInputText =
         [ Input.Props [ OnKeyDown (fun ev -> TagsKeyDown (ev.key, ev.Value) |> dispatch) ]
           Input.Placeholder "Ex: groceries"
           Input.Value model.TagsText
           Input.OnChange (fun ev -> TagsTextChanged ev.Value |> dispatch) ] @
         ([ Input.Color IsDanger ] |> addEmptyOnAlert []) |> Input.text
    
    let tagsField =
       let alertIcon =
           Icon.icon [ Icon.Size IsSmall
                       Icon.IsRight ]
                     [ Fa.i [ Fa.Solid.ExclamationTriangle ] [] ]
       
       let helpText =
           Help.help [ Help.Color IsDanger ] [ str "Duplicate tag" ]
             
       let control =
           [ tagsInputIcon
             tagsInputText
             addOnAlert alertIcon ] |>
           Control.div [ Control.HasIconLeft
                         Control.HasIconRight ]
             
       [ Label.label [] [ str "Tags" ]
         control
         addOnAlert helpText ] |>
       Field.div []
    
    let tags =
        let tagIcon name =
            Tag.tag [ Tag.Color IsInfo ] [ Icon.icon [ ] [ Fa.i [ tagIconLookup name ] [ ] ] ]
        
        let tagText name =
            Tag.tag [ Tag.Color IsLight ] [ str name ]
            
        let tagDeleteButton name =
            Tag.delete [ Tag.Props [ onClick (TagDelete name) dispatch ] ] [ ]    
        
        let tag name =
            Control.div []
                        [ Tag.list [ Tag.List.HasAddons ]
                                   [ tagText name
                                     tagIcon name
                                     tagDeleteButton name ] ]

        model.Expense.Tags |>
        List.map tag |>
        Field.div [ Field.IsGroupedMultiline ]
   
    let dateField =
        Input.date [ Input.Value (model.Expense.Date.ToString("yyyy-MM-dd"))
                     Input.Props [ onChange ChangeDate dispatch ] ] |>
        simpleField "Date" Fa.Solid.CalendarDay
    
    let payeeField =
        Input.text [ Input.Placeholder "Ex: Tesco"
                     Input.Props [ AutoFocus true; onChange ChangePayee dispatch ]
                     Input.Value model.Expense.Payee ] |>
        simpleField "Payee" Fa.Solid.CashRegister
    
    let receiptDisplayName =
        match model.SelectedFile with
        | Some file -> file
        | None      -> "No receipt selected"
    
    let fileUploadIcon =
        Icon.icon [] [ Fa.i [ Fa.Solid.Upload ] [ ] ]
        
    let uploadButton =
        File.cta []
                 [ File.icon [] [ fileUploadIcon ]
                   File.label [] [ str "Upload a receipt..." ] ]
                 
    let fileLabelChildren =
        [ File.input [ Props [ OnChange (fun ev -> FileSelected ev.Value |> dispatch) ] ]
          uploadButton
          File.name [] [ str receiptDisplayName ] ]
    
    let receiptUpload =
        Field.div []
                  [ File.file [ File.HasName ]
                              [ File.label [] fileLabelChildren ] ]
    
    form [ receiptUpload
                    
           payeeField

           dateField

           amountField
 
           inlineElements [ dropdown "Account" model.Accounts model.Expense.Account ChangeAccount
                            dropdown "Status"    expenseTypes model.Expense.Status  ChangeStatus
                            dropdown "Type"     expenseStatus model.Expense.Type    ChangeType    ]

           tagsField
           
           tags

           descriptionField ]
                
let expenseFormCard submitButtonText model dispatch =
    card [ expenseForm model dispatch ]
         (buttons dispatch submitButtonText)