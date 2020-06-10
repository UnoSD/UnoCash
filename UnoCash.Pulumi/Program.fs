module Program

open System
open System.Runtime.CompilerServices
open Pulumi
open Pulumi.Azure.ApiManagement
open Pulumi.Azure.AppInsights
open Pulumi.Azure.AppService
open Pulumi.Azure.AppService.Inputs
open Pulumi.Azure.Storage.Inputs
open Pulumi.AzureAD
open Pulumi.FSharp
open Pulumi.Azure.Core
open Pulumi.Azure.Storage
open System.Collections.Generic

let infra () =
    let resourceGroup =
        ResourceGroup "unocash"

    let whitelistIp =
        Config().Require("WhitelistIp")
    
    let storageAccount =
        Account("unocashstorage",
                AccountArgs(ResourceGroupName = io resourceGroup.Name,
                            AccountReplicationType = input "LRS",
                            AccountTier = input "Standard",
                            EnableHttpsTrafficOnly = input true),
                CustomResourceOptions(AdditionalSecretOutputs = List<string>([
                    "PrimaryAccessKey"
                    "SecondaryAccessKey"
                    "PrimaryConnectionString"
                    "PrimaryBlobConnectionString"
                    "SecondaryConnectionString"
                    "SecondaryBlobConnectionString"
                ])))
        
    let webContainer =
        Container("unocashweb",
                  ContainerArgs(StorageAccountName = io storageAccount.Name,
                                ContainerAccessType = input "private",
                                Name = input "$web"))
        
    let storageContainer =
        Container("unocashbuild",
                  ContainerArgs(StorageAccountName = io storageAccount.Name,
                                ContainerAccessType = input "private"))
    
    let appServicePlan =
        Plan("unocashasp",
             PlanArgs(ResourceGroupName = io resourceGroup.Name,
                      Kind = input "FunctionApp",
                      Sku = input (PlanSkuArgs(Tier = input "Dynamic",
                                               Size = input "Y1"))))
    
    let blob =
        Blob("unocashapi",
             BlobArgs(StorageAccountName = io storageAccount.Name,
                      StorageContainerName = io storageContainer.Name,
                      Type = input "Block",
                      Source = input ((Config().Require("ApiBuild") |> FileAsset) :> AssetOrArchive)))
    
    let codeBlobUrl =
        SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount)
    
    let appInsights =
        Insights("unocashai",
                 InsightsArgs(ResourceGroupName = io resourceGroup.Name,
                              ApplicationType = input "web",
                              RetentionInDays = input 90))
   
    let apiManagement =
        let outputs =
            TemplateDeployment("unocashapim",
                               TemplateDeploymentArgs(ResourceGroupName = io resourceGroup.Name,
                                                      TemplateBody = input (IO.File.ReadAllText("ApiManagement.json")),
                                                      Parameters = inputMap [
                                                          ("apiManagementServiceName", input "unocashapim")
                                                          ("location", io resourceGroup.Location)
                                                      ],
                                                      DeploymentMode = input "Incremental")).Outputs
        {|
            Name = outputs.Apply(fun d -> d.["name"])
            GatewayUrl = outputs.Apply(fun d -> d.["gatewayUrl"])
        |}
        
    let webContainerUrl =
        FormattableStringFactory.Create("https://{0}.blob.core.windows.net/{1}", storageAccount.Name, webContainer.Name) |>
        Output.Format
    
    let api =
        Api("unocashapimapi",
            ApiArgs(ResourceGroupName = io resourceGroup.Name,
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
                        <value>@("%s/" + context.Request.OriginalUrl.Path + context.Request.OriginalUrl.QueryString)</value>
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
             BlobArgs(StorageAccountName = io storageAccount.Name,
                      StorageContainerName = io storageContainer.Name,
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
        Output.Tuple(storageAccount.PrimaryConnectionString, storageContainer.Name, blobUrl)
              .Apply<string>(fun struct (cs, cn, bu) ->
                  Output.Create<GetAccountBlobContainerSASResult>(getSas cs cn)
                        .Apply(fun res -> bu + res.Sas))
    
    let apiPolicyXml =
        let sasToken =
            Output.Tuple(storageAccount.PrimaryConnectionString, webContainer.Name)
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
                          
        Output.Tuple(apiManagement.GatewayUrl, sasToken)
              .Apply(fun struct (gatewayUrl, st) -> tokenToPolicy st gatewayUrl)

    let mainApiPolicyBlobLink =
        apiPolicyXml.Apply(fun p -> policyBlob "mainapi" (fun _ -> p))
                    .Apply<string>(fun b -> b.Url) |>
        withSas |>
        io
    
    let _ =
        ApiPolicy("unocashapimapipolicy",
                  ApiPolicyArgs(ResourceGroupName = io resourceGroup.Name,
                                ApiManagementName = io apiManagement.Name,
                                ApiName = io api.Name,
                                XmlLink = mainApiPolicyBlobLink))
        
    let indexApiOperation =
        ApiOperation("unocashapimindexoperation",
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/",
                                      Method = input "GET",
                                      DisplayName = input "GET index",
                                      OperationId = input "get-index"))
        
    let getApiOperation =
        ApiOperation("unocashapimoperation",
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
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
                        <value>@($"https://login.microsoftonline.com/%s/oauth2/v2.0/authorize?client_id=%s&response_type=id_token&redirect_uri={System.Net.WebUtility.UrlEncode(context.Request.OriginalUrl.ToString())}&response_mode=form_post&scope=openid&nonce={Guid.NewGuid().ToString("n")}")</value>
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
    
    let getPolicyBlobLink =
        policyBlob "get" getPolicy |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let _ =
        ApiOperationPolicy("unocashapimgetoperationspolicy",
                           ApiOperationPolicyArgs(XmlLink = getPolicyBlobLink,
                                                  ApiName = io api.Name,
                                                  ApiManagementName = io apiManagement.Name,
                                                  OperationId = io getApiOperation.OperationId,
                                                  ResourceGroupName = io resourceGroup.Name))
    
    let postApiOperation =
        ApiOperation("unocashapimpostoperation",
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
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
    
    let apiFunctionPolicyBlobLink =
        policyBlob "post" postPolicy |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let _ =
        ApiOperationPolicy("unocashapimpostoperationpolicy",
                           ApiOperationPolicyArgs(XmlLink = apiFunctionPolicyBlobLink,
                                                  ApiName = io api.Name,
                                                  ApiManagementName = io apiManagement.Name,
                                                  OperationId = io postApiOperation.OperationId,
                                                  ResourceGroupName = io resourceGroup.Name))
    
    let indexPolicyXml = """
<policies>
    <inbound>
        <base />
        <rewrite-uri template="/index.html" />
        
        <validate-jwt token-value="@(context.Request.Body.As<string>().Split('&')[0].Split('=')[1])" failed-validation-httpcode="401" failed-validation-error-message="Unauthorized. Access token is missing or invalid." output-token-variable-name="jwt">
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
                        <value>@($"https://login.microsoftonline.com/%s/oauth2/v2.0/authorize?client_id=%s&response_type=id_token&redirect_uri={System.Net.WebUtility.UrlEncode(context.Request.OriginalUrl.ToString())}&response_mode=form_post&scope=openid&nonce={Guid.NewGuid().ToString("n")}")</value>
                    </set-header>
                </return-response>
            </when>
        </choose>
    </on-error>
    
</policies>
"""
    
    let _ =
        ApiOperationPolicy("unocashapimiopolicy",
                           ApiOperationPolicyArgs(ResourceGroupName = io resourceGroup.Name,
                                                  ApiManagementName = io apiManagement.Name,
                                                  ApiName = io api.Name,
                                                  OperationId = io indexApiOperation.OperationId,
                                                  XmlContent = input indexPolicyXml))
    
    let functionAppCors =
        input (FunctionAppSiteConfigCorsArgs(AllowedOrigins = inputList [ io apiManagement.GatewayUrl ],
                                             SupportCredentials = input true))
    
    let functionAppIpRestrictions =
        inputList [
            input (FunctionAppSiteConfigIpRestrictionArgs(IpAddress = input (whitelistIp + "/32")))
        ]
    
    let app =
        FunctionApp("unocashapp",
                    FunctionAppArgs(ResourceGroupName = io resourceGroup.Name,
                                    AppServicePlanId = io appServicePlan.Id,
                                    AppSettings = inputMap [ "runtime", input "dotnet"
                                                             "WEBSITE_RUN_FROM_PACKAGE", io codeBlobUrl
                                                             "APPINSIGHTS_INSTRUMENTATIONKEY", io appInsights.InstrumentationKey
                                                             "StorageAccountConnectionString", io storageAccount.PrimaryConnectionString
                                                             "FormRecognizerKey", input ""
                                                             "FormRecognizerEndpoint", input "" ],
                                    StorageAccountName = io storageAccount.Name,
                                    StorageAccountAccessKey = io storageAccount.PrimaryAccessKey,
                                    Version = input "~3",
                                    SiteConfig = input (FunctionAppSiteConfigArgs(IpRestrictions = functionAppIpRestrictions,
                                                                                  Cors = functionAppCors))))
    
    let apiFunction =
        Api("unocashapimapifunction",
            ApiArgs(ResourceGroupName = io resourceGroup.Name,
                    ApiManagementName = io apiManagement.Name,
                    DisplayName = input "API",
                    Name = input "api",
                    Path = input "api",
                    Protocols = inputList [ input "https" ],
                    Revision = input "1",
                    ServiceUrl = io (app.DefaultHostname.Apply<string>(fun hn -> sprintf "https://%s/api?code={{FunctionKey}}" hn))))
    
    let apiOperation method =
        ApiOperation("unocashapimapifunction" + method,
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
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
    
    let apiFunctionPolicyBlobLink =
        policyBlob "apifunction" apiFunctionPolicyXml |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let _ =
        ApiPolicy("unocashapimapifunctionpolicy",
                  ApiPolicyArgs(ResourceGroupName = io resourceGroup.Name,
                                ApiManagementName = io apiManagement.Name,
                                ApiName = io apiFunction.Name,
                                XmlLink = apiFunctionPolicyBlobLink))
    
    let _ =
        Blob("unocashwebconfig",
             BlobArgs(StorageAccountName = io storageAccount.Name,
                      StorageContainerName = io webContainer.Name,
                      Type = input "Block",
                      Name = input "apibaseurl",
                      Source = input (Config().Require("ApiEndpoint") |>
                                      StringAsset :>
                                      AssetOrArchive)))
    
    dict [
        ("Hostname", app.DefaultHostname :> obj)
        ("ResourceGroup", resourceGroup.Name :> obj)
        ("StorageAccount", storageAccount.Name :> obj)
        ("ApiManagementEndpoint", apiManagement.GatewayUrl :> obj)
        ("ApiManagement", apiManagement.Name :> obj)
        ("StaticWebsiteApi", api.Name :> obj)
        ("ApplicationId", spaAdApplication.ApplicationId :> obj)
        ("FunctionName", app.Name :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
