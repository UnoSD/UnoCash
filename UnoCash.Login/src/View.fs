module UnoCash.Login.View

open Feliz
open Fable.React
open UnoCash.Login.Models

let view model _ =
    Html.div [ str model.ApiBaseUrl ]