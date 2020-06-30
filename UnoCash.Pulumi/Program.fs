module Program

open Pulumi.Azure.ApiManagement.Inputs
open Pulumi.Azure.AppService.Inputs
open Pulumi.Azure.Storage.Inputs
open Pulumi.Azure.ApiManagement
open Pulumi.Azure.AppService
open Pulumi.FSharp.Output
open Pulumi.Azure.Storage
open Pulumi.FSharp.Azure
open System.Diagnostics
open System.Threading
open Pulumi.AzureAD
open Pulumi.FSharp
open System.IO
open System
open Pulumi

let infra () =
    let group =
        resourceGroup {
            name "unocash"
        }

    let storage =
        storageAccount {
            name          "unocashstorage"
            resourceGroup group
        }
        
    let webContainer =
        storageContainer {
            name          "unocashweb"
            account       storage.Name
            containerName "$web"
        }
            
    let buildContainer =
        storageContainer {
            name    "unocashbuild"
            account storage
        }
    
    let functionPlan =
        appService {
            name          "unocashasp"
            resourceGroup group
            kind          FunctionAppKind
        }
    
    let apiBlob =
        storageBlob {
            name      "unocashapi"
            account   storage
            container buildContainer
            source    (input ((Config().Require("ApiBuild") |> FileAsset) :> AssetOrArchive))
        }
    
    let codeBlobUrl =
        sasToken {
            account storage
            blob    apiBlob
        }
    
    let appInsights =
        appInsight {
            name            "unocashai"
            resourceGroup   group
            applicationType Web
            retentionInDays 90
        }
        
    let apiManagement =
        let templateOutputs =
            armTemplate {
                name          "unocashapim"
                resourceGroup group
                json          (File.ReadAllText("ApiManagement.json"))
                parameters    [ "apiManagementServiceName", input "unocashapim"
                                "location", io group.Location ]
            } |>
            fun at -> at.Outputs
        
        {| Name = output { let! outputs = templateOutputs
                           return outputs.["name"] }
           GatewayUrl = output { let! outputs = templateOutputs
                                 return outputs.["gatewayUrl"] } |}
        
    let _ =
        Logger("unocashapimlog",
               LoggerArgs(ApiManagementName = io apiManagement.Name,
                          ResourceGroupName = io group.Name,
                          ApplicationInsights = input (LoggerApplicationInsightsArgs(InstrumentationKey = io appInsights.InstrumentationKey))))
        
    let webContainerUrl =
        output {
            let! accountName = storage.Name
            let! containerName = webContainer.Name
            
            return sprintf "https://%s.blob.core.windows.net/%s" accountName containerName
        }

    let api =
        apimApi {
            name          "unocashapimapi"
            apiName       "staticwebsite"
            resourceGroup group
            apim          apiManagement.Name
            displayName   "StaticWebsite"
            protocol      HttpHttps
            serviceUrl    webContainerUrl
        }

    let tokenToPolicy (sas : string) gatewayUrl =
        let queryString =
            sas.Substring(1).Split('&') |>
            Array.map (fun pair -> pair.Split('=')) |>
            Array.map (fun arr -> (arr.[0], arr.[1])) |>
            Map.ofArray
            
        sprintf """
<policies>
    <inbound>
        <base />
        <choose>
            <when condition="@(context.Request.OriginalUrl.Scheme.ToLower() == "http")">
                <return-response>
                    <set-status code="303" reason="See Other" />
                    <set-header name="Location" exists-action="override">
                        <value>@("%s" + context.Request.OriginalUrl.Path + context.Request.OriginalUrl.QueryString)</value>
                    </set-header>
                </return-response>
            </when>
        </choose>
        <set-query-parameter name="sv" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="sr" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="st" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="se" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="sp" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="spr" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="sig" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <rate-limit calls="100" renewal-period="300" />
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
"""
         gatewayUrl
         queryString.["sv"]
         queryString.["sr"]
         queryString.["st"]
         queryString.["se"]
         queryString.["sp"]
         queryString.["spr"]
         queryString.["sig"]
        
    let containerPermissions =
        GetAccountBlobContainerSASPermissionsArgs(Read = true)
        
    let spaAdApplication =
        Application("unocashspaaadapp",
                    ApplicationArgs(ReplyUrls = inputList [ io apiManagement.GatewayUrl ],
                                    Oauth2AllowImplicitFlow = input true))
    

    let policyBlob resourceName (appIdToPolicyXml : string -> string) =
        storageBlob {
            name      ("unocash" + resourceName + "policyblob")
            account   storage
            container buildContainer
            source    (output {
                           let! appId =
                               spaAdApplication.ApplicationId
                           
                           let policyXmlAsset =
                               appIdToPolicyXml appId |>
                               StringAsset :>
                               AssetOrArchive
                               
                           return policyXmlAsset
                       } |> io)
        }
    
    let withSas (baseBlobUrl : Output<string>) =
        secretOutput {
            let! connectionString = storage.PrimaryConnectionString
            let! containerName = buildContainer.Name
            let! url = baseBlobUrl

            let start =
                DateTime.Now.ToString("u").Replace(' ', 'T')
            
            let expiry =
                DateTime.Now.AddHours(1.).ToString("u").Replace(' ', 'T')
            
            let! tokenResult =
                GetAccountBlobContainerSASArgs(
                    ConnectionString = connectionString,
                    ContainerName = containerName,
                    Start = start,
                    Expiry = expiry,
                    Permissions = containerPermissions
                ) |>
                GetAccountBlobContainerSAS.InvokeAsync

            return url + tokenResult.Sas
        }
        
    let sasExpirationOutputName = "SasTokenExpiration"
    let sasTokenOutputName = "SasToken"
    
    let stack =
        StackReference(Deployment.Instance.StackName)
    
    let sasExpirationDate =
        output {
            let! previousOutputs =
                stack.Outputs
            
            return match previousOutputs.TryGetValue sasExpirationOutputName with
                   | true, (:? string as exp) -> match DateTime.TryParse(exp) with
                                                 | true, x when x > DateTime.Now -> x, false // Unchanged
                                                 | _, _                          -> DateTime.Now.AddYears(1), true
                   | _                        -> DateTime.Now.AddYears(1), true
        }
    
    let token =
        secretOutput {
            let! (_, sasChanged) =
                sasExpirationDate
            
            return!
                match sasChanged with
                | true  -> output { let! cs = storage.PrimaryConnectionString
                                    let! cn = webContainer.Name
                                    let! (exp, _) = sasExpirationDate
                                    
                                    let args =
                                        GetAccountBlobContainerSASArgs(ConnectionString = cs,
                                                                       ContainerName = cn,
                                                                       Start = DateTime.Now
                                                                                       .ToString("u")
                                                                                       .Replace(' ', 'T'),
                                                                       Expiry = exp.ToString("u")
                                                                                   .Replace(' ', 'T'),
                                                                       Permissions = containerPermissions)
                                    
                                    // Create a Bind that accepts a Task
                                    let! st =
                                        GetAccountBlobContainerSAS.InvokeAsync(args)
                                    
                                    return st.Sas }
                | false -> output { let! tokenOutput = stack.Outputs
                                    return tokenOutput.[sasTokenOutputName] :?> string }
        }
    
    let swApiPolicyBlobLink =
        output {
            let! sas =
                token
            
            let apiPolicyXml =
                tokenToPolicy sas <| Config().Require("WebEndpoint")
            
            let blob =
                policyBlob "mainapi" (fun _ -> apiPolicyXml)
            
            return! blob.Url
        } |>
        withSas |>
        io
    
    let _ =
        ApiOperation("unocashapimindexoperation",
                     ApiOperationArgs(ResourceGroupName = io group.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/",
                                      Method = input "GET",
                                      DisplayName = input "GET index",
                                      OperationId = input "get-index"))
        
    let _ =
        ApiOperation("unocashapimoperation",
                     ApiOperationArgs(ResourceGroupName = io group.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/*",
                                      Method = input "GET",
                                      DisplayName = input "GET",
                                      OperationId = input "get"))
    
    let getPolicy applicationId =
        String.Format(File.ReadAllText("APIApimApiPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let swApiGetPolicyBlobLink =
        policyBlob "get" getPolicy |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let _ =
        ApiOperation("unocashapimpostoperation",
                     ApiOperationArgs(ResourceGroupName = io group.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/",
                                      Method = input "POST",
                                      DisplayName = input "POST AAD token",
                                      OperationId = input "post-aad-token"))
    
    let postPolicy applicationId =
        sprintf """
<policies>
    <inbound>
        <base />
        <validate-jwt token-value="@(context.Request.Body.As<string>().Split('&')[0].Split('=')[1])" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid." output-token-variable-name="jwt">
            <openid-config url="https://login.microsoftonline.com/%s/v2.0/.well-known/openid-configuration" />
            <audiences>
                <audience>%s</audience>
            </audiences>
        </validate-jwt>
        
        <return-response>
            <set-status code="303" reason="See Other" />
            <set-header name="Set-Cookie" exists-action="override">
                <value>@("jwtToken=" + context.Variables["jwt"] + "; HttpOnly")</value>
            </set-header>
            <set-header name="Location" exists-action="override">
                <value>@(context.Request.OriginalUrl.ToString())</value>
            </set-header>
        </return-response>
        
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
        <choose>
            <when condition="@(context.Response.StatusCode == 401)">
                <return-response>
                    <set-status code="401" reason="See Other" />
                    <set-body>Non puoi, rosa!</set-body>
                </return-response>
            </when>
        </choose>
    </on-error>
</policies>
"""
         Config.TenantId
         applicationId
    
    let swApiPostPolicyBlobLink =
        policyBlob "post" postPolicy |>
        (fun pb -> pb.Url) |>
        withSas |>
        io

    let indexPolicyXml applicationId =
        sprintf """
<policies>
    <inbound>
        <base />
        <rewrite-uri template="/index.html" />
        
        <validate-jwt token-value="@(context.Request.Headers.TryGetValue("Cookie", out var value) ? value?.SingleOrDefault(x => x.StartsWith("jwtToken="))?.Substring(9) : "")"
                      failed-validation-httpcode="401"
                      failed-validation-error-message="Unauthorized. Access token is missing or invalid."
                      output-token-variable-name="jwt">
            <openid-config url="https://login.microsoftonline.com/%s/v2.0/.well-known/openid-configuration" />
            <audiences>
                <audience>%s</audience>
            </audiences>
        </validate-jwt>
        
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    
    <on-error>
        <base />
        <choose>
            <when condition="@(context.Response.StatusCode == 401)">
                <return-response>
                    <set-status code="303" reason="See Other" />
                    <set-header name="Location" exists-action="override">
                        <value>@($"https://login.microsoftonline.com/%s/oauth2/v2.0/authorize?client_id=%s&response_type=id_token&redirect_uri={System.Net.WebUtility.UrlEncode(context.Request.OriginalUrl.ToString())}&response_mode=form_post&scope=openid%%20profile&nonce={Guid.NewGuid().ToString("n")}")</value>
                    </set-header>
                </return-response>
            </when>
        </choose>
    </on-error>
    
</policies>
"""
         Config.TenantId
         applicationId
         Config.TenantId
         applicationId
    
    let swApiGetIndexPolicyBlobLink =
        policyBlob "getindex" indexPolicyXml |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let functionAppCors =
        input (FunctionAppSiteConfigCorsArgs(AllowedOrigins = inputList [ io apiManagement.GatewayUrl ],
                                             SupportCredentials = input true))
    
    let app =
        FunctionApp("unocashapp",
                    FunctionAppArgs(ResourceGroupName = io group.Name,
                                    AppServicePlanId = io functionPlan.Id,
                                    AppSettings = inputMap [ "runtime", input "dotnet"
                                                             "WEBSITE_RUN_FROM_PACKAGE", io codeBlobUrl
                                                             "APPINSIGHTS_INSTRUMENTATIONKEY", io appInsights.InstrumentationKey
                                                             "StorageAccountConnectionString", io storage.PrimaryConnectionString
                                                             "FormRecognizerKey", input ""
                                                             "FormRecognizerEndpoint", input "" ],
                                    StorageAccountName = io storage.Name,
                                    StorageAccountAccessKey = io storage.PrimaryAccessKey,
                                    Version = input "~3",
                                    SiteConfig = input (FunctionAppSiteConfigArgs(Cors = functionAppCors))))
    
    let apiFunction =
        apimApi {
            name          "unocashapimapifunction"
            apiName       "api"
            path          "api"
            resourceGroup group
            apim          apiManagement.Name
            displayName   "API"
            protocol      Https
            serviceUrl    (app.DefaultHostname.Apply (sprintf "https://%s"))
        }
    
    let apiOperation method =
        ApiOperation("unocashapimapifunction" + method,
                     ApiOperationArgs(ResourceGroupName = io group.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io apiFunction.Name,
                                      UrlTemplate = input "/*",
                                      Method = input (method.ToUpper()),
                                      DisplayName = input (method.ToUpper()),
                                      OperationId = input method))
    
    let _ =
        [ "get"; "post"; "delete"; "put" ] |>
        List.map apiOperation
    
    let apiFunctionPolicyXml applicationId =
        sprintf """
<policies>
    <inbound>
        <base />
        
        <validate-jwt token-value="@(context.Request.Headers.TryGetValue("Cookie", out var value) ? value?.SingleOrDefault(x => x.StartsWith("jwtToken="))?.Substring(9) : "")"
                      failed-validation-httpcode="401"
                      failed-validation-error-message="Unauthorized. Access token is missing or invalid."
                      output-token-variable-name="jwt">
            <openid-config url="https://login.microsoftonline.com/%s/v2.0/.well-known/openid-configuration" />
            <audiences>
                <audience>%s</audience>
            </audiences>
        </validate-jwt>
        
        <set-header name="x-functions-key" exists-action="override"><value>{{FunctionKey}}</value></set-header>
    </inbound>
    <backend>
        <base />
    </backend>
    <outbound>
        <base />
    </outbound>
    <on-error>
        <base />
    </on-error>
</policies>
"""
         Config.TenantId
         applicationId
    
    let functionApiPolicyBlobLink =
        policyBlob "functionapi" apiFunctionPolicyXml |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    storageBlob {
        name      "unocashwebconfig"
        blobName  "apibaseurl"
        account   storage
        container webContainer
        blobType  Block
        source    (Config().Require("WebEndpoint") + "/api" |>
                   StringAsset :>
                   AssetOrArchive |>
                   input)
        
    } |> ignore
    
    let sasExpirationDateString =
        output {
            let! (date, _) = sasExpirationDate
            
            return date.ToString("u")
        }
    
    dict [
        "Hostname",                           app.DefaultHostname            :> obj
        "ResourceGroup",                      group.Name                        :> obj
        "StorageAccount",                     storage.Name                   :> obj
        "ApiManagementEndpoint",              apiManagement.GatewayUrl       :> obj
        "ApiManagement",                      apiManagement.Name             :> obj
        "StaticWebsiteApi",                   api.Name                       :> obj
        "FunctionApi",                        apiFunction.Name               :> obj
        "ApplicationId",                      spaAdApplication.ApplicationId :> obj
        "FunctionName",                       app.Name                       :> obj
        
        // Outputs to read on next deployment to check for changes
        sasTokenOutputName,                   token                          :> obj
        sasExpirationOutputName,              sasExpirationDateString        :> obj
                                                                       
        "StaticWebsiteApiPolicyLink",         swApiPolicyBlobLink            :> obj
        "StaticWebsiteApiPostPolicyLink",     swApiPostPolicyBlobLink        :> obj
        "StaticWebsiteApiGetPolicyLink",      swApiGetPolicyBlobLink         :> obj
        "StaticWebsiteApiGetIndexPolicyLink", swApiGetIndexPolicyBlobLink    :> obj
        "FunctionApiPolicyLink",              functionApiPolicyBlobLink      :> obj
    ]

[<EntryPoint>]
let main _ =
  let rec waitForDebugger () =
      match Debugger.IsAttached with
      | false -> Thread.Sleep(100)
                 printf "."
                 waitForDebugger ()
      | true  -> printfn " attached"
  
  match Environment.GetEnvironmentVariable("PULUMI_DEBUG_WAIT") = "1" with
  | true -> printf "Awaiting debugger to attach to the process"
            waitForDebugger ()
  | _    -> ()
  
  Deployment.run infra
