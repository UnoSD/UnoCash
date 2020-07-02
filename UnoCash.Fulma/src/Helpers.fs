module UnoCash.Fulma.Helpers

open System
open Fable.React.Props
open Fable.React
open Fulma
open Fable.FontAwesome

let onChange message dispatch =
    OnChange (fun event -> message event.Value |> dispatch)
    
let onClick message dispatch =
    OnClick (fun _ -> message |> dispatch)

let card content footer =
    Card.card [ ]
              [ Card.content [ ]
                             [ Content.content [] content
                               footer ] ]

let form<'a> =
    Standard.form []

let dropdownWithEvent title items value msgOnChange dispatch =
    let options items =
        items |>
        Seq.map (fun item -> option [ Value item ] [ str item ])

    Field.div [ Field.Props [ Style [ MarginRight "20px"; MarginBottom "10px" ] ] ]
              [ Label.label [ ] [ str title ]
                Control.div [ ]
                            [ Select.select [ ]
                                            [ select [ Value value
                                                       onChange msgOnChange dispatch ]
                                                     (options items) ] ] ]

[<AbstractClass; Sealed>]
type Fulma private () =
    static member card(content, footer) =
        Card.card []
                  [ Card.content []
                                 [ Content.content [] content ]
                    footer ]
                  
    static member card(content) =
        Card.card []
                  [ Card.content []
                                 [ Content.content [] content ] ]

let pascalCaseToDisplay text =
    let folder str =
        function
        | c when Char.IsUpper c && (str = "") -> string c
        | c when Char.IsUpper c               -> Char.ToLower c |> sprintf "%s %c" str
        | c                                   -> sprintf "%s%c" str c
    text |>
    Seq.fold folder String.Empty
        
type Overloads = Overloads

type Overloads with
    static member div =
        Standard.div []
   
let toDecimal value precision =
    let precisionMultiplier =
        pown 10m precision
    match Decimal.TryParse(value) with
    | true, dec -> (dec * precisionMultiplier |> Decimal.Truncate) / precisionMultiplier
    | false, _  -> 0m

let isSmallScreen =
    Browser.Dom.window.screen.width <= 768.
    
let private spinner =
    let spinnerIcon =
        Fa.i [ Fa.Solid.Sync; Fa.Spin ] []
    
    div [ Class ("block " + Fa.Classes.Size.Fa3x)
          Style [ TextAlign TextAlignOptions.Center ] ]
        [ spinnerIcon ]
    
let spinnerOrContent loaded content =
    match loaded with
    | true  -> content ()
    | false -> spinner