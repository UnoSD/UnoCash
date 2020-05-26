module Program

open Pulumi
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
        input (Config().Require("WhitelistIp"))
    
    let networkRules =
        Inputs.AccountNetworkRulesArgs(IpRules = inputList [ whitelistIp ],
                                       DefaultAction = input "Allow")
    
    let storageAccount =
        Account("unocashstorage",
                AccountArgs(ResourceGroupName = io resourceGroup.Name,
                            AccountReplicationType = input "LRS",
                            AccountTier = input "Standard",
                            EnableHttpsTrafficOnly = input true,
                            StaticWebsite = input (AccountStaticWebsiteArgs(IndexDocument = input "index.html")),
                            NetworkRules = input networkRules))
        
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
                              ApplicationType = input "web"))
    
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
                                    SiteConfig = input (FunctionAppSiteConfigArgs(IpRestrictions = inputList [
                                        input (FunctionAppSiteConfigIpRestrictionArgs(IpAddress = whitelistIp))
                                    ]))))
    
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
    
    dict [
        ("Hostname", app.DefaultHostname :> obj)
        ("StorageAccount", storageAccount.Name :> obj)
        ("SiteEndpoint", storageAccount.PrimaryWebEndpoint :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
