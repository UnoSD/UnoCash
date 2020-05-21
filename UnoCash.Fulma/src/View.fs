module UnoCash.Fulma.View

open UnoCash.Fulma.Helpers
open UnoCash.Fulma.Tabs
open UnoCash.Fulma.Pages

let view model dispatch =
    div [ tabs model dispatch
          page model dispatch ]