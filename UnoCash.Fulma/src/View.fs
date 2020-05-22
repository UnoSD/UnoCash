module UnoCash.Fulma.View

open UnoCash.Fulma.Tabs
open UnoCash.Fulma.Pages
open Feliz

let view model dispatch =
    Html.div [ tabs model dispatch
               page model dispatch ]