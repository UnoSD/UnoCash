module UnoCash.Fulma.About

open Fulma
open UnoCash.Fulma.Helpers
open Fable.React

let private buttons =
    Card.footer []
                [ Card.Footer.a []
                                [ str "Contact" ]
                  Card.Footer.a []
                                [ str "GitHub" ]
                  Card.Footer.a []
                                [ str "Blog" ] ]

let aboutCard _ _ =
    let modifiers =
        Container.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ]
        
    let subText =
        "Personal finance, expenses tracking, splitting, receipt photos automatic recognition"
        
    card [ Hero.body []
                     [ Container.container [ Container.IsFluid
                                             modifiers ]
                     [ Heading.h1 [] [ str "UnoCash" ]
                       Heading.h4 [ Heading.IsSubtitle ] [ str subText ] ] ] ]
         buttons