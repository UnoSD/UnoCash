module UnoCash.Login.Models

open Fable.Import
open Fable.Import.Msal
open Fable.Core.JsInterop
open Browser.Dom

type Model =
    {
        ApiBaseUrl : string
    }
    
let emptyModel = 
    {
        ApiBaseUrl = "http://localhost:7071"
    }
    
let private msalConfig () : Configuration.Configuration =
    let authSettings : Configuration.AuthOptions =
        !!{|
            clientId = "[your client Id]"
            navigateToLoginRequestUrl = Some true
            redirectUri = (Some window.location.origin)
            postLogoutRedirectUri = (Some window.location.origin)
            authority = Some "https://login.microsoftonline.com/[your tenantId]"
        |}

    let cacheSettings : Configuration.CacheOptions =
        !!{|
            cacheLocation = Some Configuration.CacheLocation.LocalStorage
        |}

    let config:Msal.Configuration.Configuration =
        !!{| 
            auth=authSettings
            cache=Some cacheSettings
        |}
    
    config
    
let userAgent =
    let config = msalConfig ()
    UserAgentApplication.userAgentApplication.Create(config)
    
let getAuthenticationParameters (account:Account.Account option) : AuthenticationParameters.AuthenticationParameters =
    !!{|
        redirectUri = (Some window.location.origin)
        account = account
        scopes = [| "https://management.azure.com//user_impersonation"|]
    |}