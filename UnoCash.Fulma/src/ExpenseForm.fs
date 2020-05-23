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
open Fable.Core.JsInterop
open System

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
        div [ Style [ Display DisplayOptions.Flex
                      FlexFlow "row wrap" ] ]
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
        simpleField "Amount" Fa.Solid.PoundSign
    
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
        let date =
            match model.Expense.Date.ToString("yyyy-MM-dd") with
            | "01-01-01" -> DateTime.Today.ToString("yyyy-MM-dd")
            | d          -> d
        
        Input.date [ Input.Value date
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
        let icon =
            match model.ReceiptAnalysis.Status with
            | NotStarted -> [ Fa.Solid.Upload ]
            | InProgress -> [ Fa.Solid.Spinner; Fa.Spin ]
            | Completed  -> [ Fa.Solid.Check ]
        
        Icon.icon [] [ Fa.i icon [ ] ]
        
    let uploadButton =
        File.cta []
                 [ File.icon [] [ fileUploadIcon ]
                   File.label [] [ str "Upload a receipt..." ] ]
                 
    let uploadFile (ev : Browser.Types.Event) =
        let reader = Browser.Dom.FileReader.Create()
        
        //Show progress with Browser.Types.ProgressEvent
        
        let file = ev.target?files?(0)
        
        dispatch (FileSelected (file?name))
        
        reader.onload <- (fun evt -> FileUpload (evt.target?result, file?name, evt?total) |> dispatch)
        
        reader.readAsArrayBuffer(file)
                 
    let fileLabelChildren =
        [ File.input [ Props [ OnInput uploadFile ] ]
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