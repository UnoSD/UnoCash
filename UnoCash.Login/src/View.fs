module UnoCash.Login.View

open Feliz
open UnoCash.Login.Messages

let view _ dispatch =
    Html.div [ Html.button [
               prop.text "Login"
               prop.onClick (fun _ -> dispatch Login) ] ]