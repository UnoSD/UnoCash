module Program

open Pulumi
open Pulumi.Azure.AppService
open Pulumi.Azure.AppService.Inputs
open Pulumi.FSharp
open Pulumi.Azure.Core
open Pulumi.Azure.Storage

let infra () =
    let resourceGroup = ResourceGroup "UnoCash"

    let storageAccount =
        Account("unocashstorage",
                AccountArgs(ResourceGroupName = io resourceGroup.Name,
                            AccountReplicationType = input "LRS",
                            AccountTier = input "Standard"))

    let storageContainer =
        Container("zips",
                  ContainerArgs(StorageAccountName = io storageAccount.Name,
                                ContainerAccessType = input "private"))
    
    let appServicePlan =
        Plan("unocashasp",
             PlanArgs(ResourceGroupName = io resourceGroup.Name,
                      Kind = input "FunctionApp",
                      Sku = input (PlanSkuArgs(Tier = input "Dynamic",
                                               Size = input "Y1"))))
    
    let blob =
        Blob("zip",
             BlobArgs(StorageAccountName = io storageAccount.Name,
                      StorageContainerName = io storageContainer.Name,
                      Type = input "Block",
                      Source = input (FileArchive("../UnoCash.Api/bin/Debug/netcoreapp3.1/publish") :> AssetOrArchive)))
    
    let codeBlobUrl =
        SharedAccessSignature.SignedBlobReadUrl(blob, storageAccount)
    
    let app =
        FunctionApp("unocashapp",
                    FunctionAppArgs(ResourceGroupName = io resourceGroup.Name,
                                    AppServicePlanId = io appServicePlan.Id,
                                    AppSettings = inputMap [ "runtime", input "dotnet"
                                                             "WEBSITE_RUN_FROM_PACKAGE", io codeBlobUrl ],
                                    StorageAccountName = io storageAccount.Name,
                                    StorageAccountAccessKey = io storageAccount.PrimaryAccessKey,
                                    Version = input "~3"))
    
    dict [
        ("Hostname", app.DefaultHostname :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
