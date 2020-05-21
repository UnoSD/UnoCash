module UnoCash.Fulma.App

open Elmish
open Elmish.HMR
open Elmish.Debug
open UnoCash.Fulma.Updates
open UnoCash.Fulma.View

Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run