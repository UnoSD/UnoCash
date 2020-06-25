﻿module Program

open System
open System.Runtime.CompilerServices
open Pulumi
open Pulumi.Azure.ApiManagement
open Pulumi.Azure.ApiManagement.Inputs
open Pulumi.Azure.AppService
open Pulumi.Azure.AppService.Inputs
open Pulumi.Azure.Storage.Inputs
open Pulumi.AzureAD
open Pulumi.FSharp
open Pulumi.Azure.Core
open Pulumi.Azure.Storage
open Pulumi.FSharp.Azure

let infra () =
    let rg =
        resourceGroup {
            name "unocash"
        }

    let storage =
        storageAccount {
            name          "unocashstorage"
            resourceGroup rg
            replication   LRS
            tier          Standard
            httpsOnly     true
        }
        
    let webContainer =
        storageContainer {
            name           "unocashweb"
            storageAccount storage.Name
            access         Private
            containerName  "$web"
        }
            
    let buildContainer =
        storageContainer {
            name           "unocashbuild"
            storageAccount storage
        }
    
    let appServicePlan =
        functionAppService {
            name          "unocashasp"
            resourceGroup rg
        }
    
    let blob =
        storageBlob {
            name           "unocashapi"
            storageAccount storage
            container      buildContainer
            source         (input ((Config().Require("ApiBuild") |> FileAsset) :> AssetOrArchive))
        }
    
    let codeBlobUrl =
        SharedAccessSignature.SignedBlobReadUrl(blob, storage)
    
    let appInsights =
        appInsight {
            name            "unocashai"
            resourceGroup   rg
            applicationType AppInsightPrivate.Web
            retentionInDays 90
        }
        
    let apiManagement =
        let outputs =
            TemplateDeployment("unocashapim",
                               TemplateDeploymentArgs(ResourceGroupName = io rg.Name,
                                                      TemplateBody = input (IO.File.ReadAllText("ApiManagement.json")),
                                                      Parameters = inputMap [
                                                          ("apiManagementServiceName", input "unocashapim")
                                                          ("location", io rg.Location)
                                                      ],
                                                      DeploymentMode = input "Incremental")).Outputs
        {|
            Name = outputs.Apply(fun d -> d.["name"])
            GatewayUrl = outputs.Apply(fun d -> d.["gatewayUrl"])
        |}
        
    let _ =
        Logger("unocashapimlog",
               LoggerArgs(ApiManagementName = io apiManagement.Name,
                          ResourceGroupName = io rg.Name,
                          ApplicationInsights = input (LoggerApplicationInsightsArgs(InstrumentationKey = io appInsights.InstrumentationKey))))
        
    let webContainerUrl =
        FormattableStringFactory.Create("https://{0}.blob.core.windows.net/{1}", storage.Name, webContainer.Name) |>
        Output.Format
    
    let api =
        Api("unocashapimapi",
            ApiArgs(ResourceGroupName = io rg.Name,
                    ApiManagementName = io apiManagement.Name,
                    DisplayName = input "StaticWebsite",
                    Name = input "staticwebsite",
                    Path = input "",
                    Protocols = inputList [ input "https"; input "http" ],
                    Revision = input "1",
                    ServiceUrl = io webContainerUrl(*,
                    SubscriptionRequired = false*)))

    let tokenToPolicy (tokenResult : GetAccountBlobContainerSASResult) gatewayUrl =
        let queryString =
            tokenResult.Sas.Substring(1).Split('&') |>
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
    
    let policyBlob name (appIdToPolicyXml : string -> string) =
        Blob("unocash" + name + "policyblob",
             BlobArgs(StorageAccountName = io storage.Name,
                      StorageContainerName = io buildContainer.Name,
                      Type = input "Block",
                      Source = (appIdToPolicyXml |>
                                spaAdApplication.ApplicationId.Apply |>
                                (fun o -> o.Apply (fun x -> x |> StringAsset :> AssetOrArchive)) |>
                                io)))
    
    let getSas connectionString containerName =
        GetAccountBlobContainerSASArgs(ConnectionString = connectionString,
                                       ContainerName = containerName,
                                       Start = DateTime.Now
                                                       .ToString("u")
                                                       .Replace(' ', 'T'),
                                       Expiry = DateTime.Now
                                                        .AddHours(1.)
                                                        .ToString("u")
                                                        .Replace(' ', 'T'),
                                       Permissions = containerPermissions) |>
        GetAccountBlobContainerSAS.InvokeAsync
    
    let withSas blobUrl =
        Output.Tuple(storage.PrimaryConnectionString, buildContainer.Name, blobUrl)
              .Apply<string>(fun struct (cs, cn, bu) ->
                  Output.Create<GetAccountBlobContainerSASResult>(getSas cs cn)
                        .Apply(fun res -> bu + res.Sas))
    
    let apiPolicyXml =
        // Is it possible for Pulumi to read the current state
        // check if the token is not expired
        // if not, don't alter it
        // if yes, alter it and regenerate
        // Pulumi reflection-like
        
        
        let sasToken =
            // Replace Apply with CE
            Output.Tuple(storage.PrimaryConnectionString, webContainer.Name)
                          .Apply(fun struct (cs, cn) ->
                                     GetAccountBlobContainerSASArgs(ConnectionString = cs,
                                                                    ContainerName = cn,
                                                                    Start = DateTime.Now
                                                                                    .ToString("u")
                                                                                    .Replace(' ', 'T'),
                                                                    Expiry = DateTime.Now
                                                                                     .AddYears(1)
                                                                                     .ToString("u")
                                                                                     .Replace(' ', 'T'),
                                                                    Permissions = containerPermissions))
                          .Apply<GetAccountBlobContainerSASResult>(GetAccountBlobContainerSAS.InvokeAsync)
                          
        sasToken.Apply(fun st -> tokenToPolicy st (Config().Require("WebEndpoint")))

    let swApiPolicyBlobLink =
        // Use that to check if expired
        //Blob.Get("", input "unocashmainapipolicyblob").Metadata.Apply(fun x -> x.["date"])
        //
        //StackReference("").Outputs.Apply(fun x -> x.["LastTokenDate"])
        
        apiPolicyXml.Apply(fun p -> policyBlob "mainapi" (fun _ -> p))
                    .Apply<string>(fun b -> b.Url) |>
        withSas |>
        io
    
    let _ =
        ApiOperation("unocashapimindexoperation",
                     ApiOperationArgs(ResourceGroupName = io rg.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/",
                                      Method = input "GET",
                                      DisplayName = input "GET index",
                                      OperationId = input "get-index"))
        
    let _ =
        ApiOperation("unocashapimoperation",
                     ApiOperationArgs(ResourceGroupName = io rg.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/*",
                                      Method = input "GET",
                                      DisplayName = input "GET",
                                      OperationId = input "get"))
    
    let getPolicy applicationId =
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
    
    let swApiGetPolicyBlobLink =
        policyBlob "get" getPolicy |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let _ =
        ApiOperation("unocashapimpostoperation",
                     ApiOperationArgs(ResourceGroupName = io rg.Name,
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
                    FunctionAppArgs(ResourceGroupName = io rg.Name,
                                    AppServicePlanId = io appServicePlan.Id,
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
        Api("unocashapimapifunction",
            ApiArgs(ResourceGroupName = io rg.Name,
                    ApiManagementName = io apiManagement.Name,
                    DisplayName = input "API",
                    Name = input "api",
                    Path = input "api",
                    Protocols = inputList [ input "https" ],
                    Revision = input "1",
                    ServiceUrl = io (app.DefaultHostname.Apply<string>(fun hn -> sprintf "https://%s" hn))))
    
    let apiOperation method =
        ApiOperation("unocashapimapifunction" + method,
                     ApiOperationArgs(ResourceGroupName = io rg.Name,
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
    
    let _ =
        Blob("unocashwebconfig",
             BlobArgs(StorageAccountName = io storage.Name,
                      StorageContainerName = io webContainer.Name,
                      Type = input "Block",
                      Name = input "apibaseurl",
                      Source = input (Config().Require("WebEndpoint") + "/api" |>
                                      StringAsset :>
                                      AssetOrArchive)))
    
    dict [
        ("Hostname", app.DefaultHostname :> obj)
        ("ResourceGroup", rg.Name :> obj)
        ("StorageAccount", storage.Name :> obj)
        ("ApiManagementEndpoint", apiManagement.GatewayUrl :> obj)
        ("ApiManagement", apiManagement.Name :> obj)
        ("StaticWebsiteApi", api.Name :> obj)
        ("FunctionApi", apiFunction.Name :> obj)
        ("ApplicationId", spaAdApplication.ApplicationId :> obj)
        ("FunctionName", app.Name :> obj)
        
        ("StaticWebsiteApiPolicyLink", swApiPolicyBlobLink :> obj)
        ("StaticWebsiteApiPostPolicyLink", swApiPostPolicyBlobLink :> obj)
        ("StaticWebsiteApiGetPolicyLink", swApiGetPolicyBlobLink :> obj)
        ("StaticWebsiteApiGetIndexPolicyLink", swApiGetIndexPolicyBlobLink :> obj)
        ("FunctionApiPolicyLink", functionApiPolicyBlobLink :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
