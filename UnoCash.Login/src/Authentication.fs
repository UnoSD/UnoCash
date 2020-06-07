module UnoCash.Login.Authentication

open Fable.Import
open Fable.Import.Msal
open Fable.Core.JsInterop
open Browser.Dom

let private msalConfig: Configuration.Configuration =
    let authSettings: Configuration.AuthOptions =
        !!{| clientId = "392c36cd-3617-44c4-aa80-d2361e610815"
             authority = Some "https://login.microsoftonline.com/1a3a38b8-ebe3-4af5-a56b-69aec18310dc"
             redirectUri = Some window.location.origin
             postLogoutRedirectUri = Some window.location.origin |}

    let cacheSettings: Configuration.CacheOptions =
        !!{| cacheLocation = Some Configuration.CacheLocation.LocalStorage
             forceRefresh = false |}

    !!{| auth = authSettings
         cache = Some cacheSettings |}


let userAgent =
    UserAgentApplication.userAgentApplication.Create(msalConfig)

let getAuthenticationParameters (account: Account.Account option): AuthenticationParameters.AuthenticationParameters =
    !!{| redirectUri = Some window.location.origin
         account = account
         scopes = [| "https://management.azure.com/user_impersonation" |]
         extraScopesToConsent = Array.empty<string> |}

let aquireToken userRequest =
    Async.FromContinuations <| fun (resolve, reject, _) ->
        userAgent.acquireTokenSilent(userRequest).``then``(fun response -> resolve response).catch(fun error ->
                 let errorMessage: string = error?errorMessage
                 if (errorMessage.Contains("interaction_required")) then
                     userAgent.acquireTokenPopup(userRequest).``then``(fun response -> resolve response)
                              .catch(fun error -> reject (exn error?errorMessage)) |> ignore
                 else
                     reject (exn errorMessage))
        |> ignore
