module UnoCash.Login.Updates

open Elmish
open UnoCash.Login.Models
open UnoCash.Login.Messages
open Fetch

let private loadConfig defaultBaseUrl =
    tryFetch "/apibaseurl" [] |>
    Promise.bind (fun x -> match x with
                           | Ok y -> y.text()
                           | Error _ -> Promise.lift defaultBaseUrl)

let init _ =
    emptyModel, Cmd.OfPromise.perform loadConfig emptyModel.ApiBaseUrl SetApiBaseUrl

let update message model =
    match message with
    | SetApiBaseUrl apiHost     -> { model with ApiBaseUrl = apiHost }, Cmd.none