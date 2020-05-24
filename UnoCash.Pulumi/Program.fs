module Program

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

    dict [
        ("ResourceGroupName", resourceGroup.Name :> obj)
        ("StorageAccountName", storageAccount.Name :> obj)
    ]

[<EntryPoint>]
let main _ =
  Deployment.run infra
