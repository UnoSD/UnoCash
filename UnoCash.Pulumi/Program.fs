module Program

open System.Runtime.CompilerServices
open Pulumi
open Pulumi.Azure.ApiManagement
open Pulumi.Azure.AppInsights
open Pulumi.Azure.AppService
open Pulumi.Azure.AppService.Inputs
open Pulumi.Azure.Storage.Inputs
open Pulumi.FSharp
open Pulumi.Azure.Core
open Pulumi.Azure.Storage

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
                            EnableHttpsTrafficOnly = input true,
                            StaticWebsite = input (AccountStaticWebsiteArgs(IndexDocument = input "index.html"))))
        
    let _ =
        AccountNetworkRules("unocashsafw",
                            AccountNetworkRulesArgs(IpRules = inputList [ input whitelistIp ],
                                                    DefaultAction = input "Allow",
                                                    StorageAccountName = io storageAccount.Name,
                                                    ResourceGroupName = io resourceGroup.Name))
        
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
    
    let functionAppCors =
        input (FunctionAppSiteConfigCorsArgs(AllowedOrigins = inputList [ io storageAccount.PrimaryWebEndpoint ],
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
    
    let _ =
        Blob("unocashwebconfig",
             BlobArgs(StorageAccountName = io storageAccount.Name,
                      StorageContainerName = input "$web",
                      Type = input "Block",
                      Name = input "apibaseurl",
                      Source = io (app.DefaultHostname.Apply (fun x -> x |>
                                                                       sprintf "https://%s" |>
                                                                       StringAsset :>
                                                                       AssetOrArchive))))
    
    let apiManagement =
        Service("unocashapim",
                ServiceArgs(ResourceGroupName = io resourceGroup.Name,
                            SkuName = input "Consumption_1",
                            PublisherName = input "UnoSD",
                            PublisherEmail = input "info"))
    
    let webContainerUrl =
        FormattableStringFactory.Create("https://{0}.blob.core.windows.net/$web", storageAccount.Name) |>
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
                    ServiceUrl = io webContainerUrl))

    let tokenToPolicy (tokenResult : GetAccountBlobContainerSASResult) =
        sprintf """
<policies>
    <inbound>
        <base />
        <choose>
            <when condition="@(context.Request.OriginalUrl.Scheme.ToLower() == "http")">
                <return-response>
                    <set-status code="303" reason="See Other" />
                    <set-header name="Location" exists-action="override">
                        <value>@("https://pizza.azure-api.net/" + context.Request.OriginalUrl.Path + context.Request.OriginalUrl.QueryString)</value>
                    </set-header>
                </return-response>
            </when>
        </choose>
        <set-query-parameter name="sv" exists-action="override">
            <value>2019-10-10</value>
        </set-query-parameter>
        <set-query-parameter name="ss" exists-action="override">
            <value>b</value>
        </set-query-parameter>
        <set-query-parameter name="srt" exists-action="override">
            <value>sco</value>
        </set-query-parameter>
        <set-query-parameter name="sp" exists-action="override">
            <value>rx</value>
        </set-query-parameter>
        <set-query-parameter name="se" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="st" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <set-query-parameter name="spr" exists-action="override">
            <value>https</value>
        </set-query-parameter>
        <set-query-parameter name="sig" exists-action="override">
            <value>%s</value>
        </set-query-parameter>
        <!--        <set-variable name="APIVersion" value="2012-02-12" />
        <set-variable name="UTCNow" value="@(DateTime.UtcNow.ToString("R"))" />
        <set-variable name="RequestPath" value="@(context.Request.Url.Path)" />
        <set-header name="x-ms-date" exists-action="override">
            <value>@(context.Variables.GetValueOrDefault<string>("UTCNow"))</value>
        </set-header>
        <set-header name="Authorization" exists-action="override">
            <value>@{
                    var account = "STORAGE ACCOUNT NAME";
                    var key = "STORAGE ACCOUNT KEY";
                    var splitPath = context.Variables.GetValueOrDefault<string>("RequestPath").Split('/');
                    var container = "$web";
                    var file = splitPath.Last();
                    var dateToSign = context.Variables.GetValueOrDefault<string>("UTCNow");
                    var stringToSign = string.Format("GET\n\n\n{0}\n/{1}/{2}/{3}", dateToSign, account, container, file);
                    string signature;
                    var unicodeKey = Convert.FromBase64String(key);
                    using (var hmacSha256 = new HMACSHA256(unicodeKey))
                    {
                        var dataToHmac = Encoding.UTF8.GetBytes(stringToSign);
                        signature = Convert.ToBase64String(hmacSha256.ComputeHash(dataToHmac));
                    }
                    var authorizationHeader = string.Format(
                        "{0} {1}:{2}",
                        "SharedKey",
                        account,
                        signature);
                    return authorizationHeader;
                }</value>
        </set-header>-->
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
         tokenResult.Expiry
         tokenResult.Start
         tokenResult.Sas
        
    let sasToken =
        storageAccount.PrimaryConnectionString
                      .Apply(fun cs -> GetAccountBlobContainerSASArgs(ConnectionString = cs,
                                                                      ContainerName = "$web"))
                      .Apply<GetAccountBlobContainerSASResult>(GetAccountBlobContainerSAS.InvokeAsync)
                      .Apply(tokenToPolicy)

    let _ =
        ApiPolicy("unocashapimapipolicy",
                  ApiPolicyArgs(ResourceGroupName = io resourceGroup.Name,
                                ApiManagementName = io apiManagement.Name,
                                ApiName = io api.Name,
                                XmlContent = io sasToken))
        
    let indexApiOperation =
        ApiOperation("unocashapimindexoperation",
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/",
                                      Method = input "GET",
                                      DisplayName = input "GET index",
                                      OperationId = input "get-index"))
        
    let _ =
        ApiOperation("unocashapimoperation",
                     ApiOperationArgs(ResourceGroupName = io resourceGroup.Name,
                                      ApiManagementName = io apiManagement.Name,
                                      ApiName = io api.Name,
                                      UrlTemplate = input "/*",
                                      Method = input "GET",
                                      DisplayName = input "GET",
                                      OperationId = input "get"))
    
    let indexPolicyXml = """
<policies>
    <inbound>
        <base />
        <rewrite-uri template="/index.html" />
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
    
    let _ =
        ApiOperationPolicy("unocashapimiopolicy",
                           ApiOperationPolicyArgs(ResourceGroupName = io resourceGroup.Name,
                                                  ApiManagementName = io apiManagement.Name,
                                                  ApiName = io api.Name,
                                                  OperationId = io indexApiOperation.OperationId,
                                                  XmlContent = input indexPolicyXml))
    
    dict [
        ("Hostname", app.DefaultHostname :> obj)
        ("StorageAccount", storageAccount.Name :> obj)
        ("SiteEndpoint", storageAccount.PrimaryWebEndpoint :> obj)
        ("ApiManagementEndpoint", apiManagement.GatewayUrl :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
