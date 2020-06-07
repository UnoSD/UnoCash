module UnoCash.Login.App

open Elmish
open Elmish.HMR
open Elmish.Debug
open UnoCash.Login.Updates
open UnoCash.Login.View

Program.mkProgram init update view
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run