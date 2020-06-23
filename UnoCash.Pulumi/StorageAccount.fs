[<AutoOpen>]
module Pulumi.FSharp.Azure.StorageAccount

open System.Collections.Generic
open Pulumi.Azure.Storage
open Pulumi.FSharp.Azure
open Pulumi.Azure.Core
open Pulumi.FSharp
open Pulumi

type ResourceGroupArg =
    | Object of ResourceGroup
    | Name of string

type Replication =
    | LRS
    
type Tier =
    | Standard

type StorageAccountArgsRecord = {
    Name: string
    Region: Region
    Tags: (string * Input<string>) list
    ResourceGroup: ResourceGroupArg
    Replication: Replication
    Tier: Tier
    HttpsOnly: bool
}

type StorageAccountBuilder internal () =
    member __.Yield _ = {
        Name = ""
        Region = WestEurope
        Tags = []
        ResourceGroup = Name ""
        Replication = LRS
        Tier = Standard
        HttpsOnly = true
    }

    member __.Run args =
        args.Region |>
        regionName |>
        input |>
        (fun l  -> AccountArgs(Location = l,
                               ResourceGroupName = (match args.ResourceGroup with
                                                    | Object rg -> io rg.Name
                                                    | Name n -> input n),
                               AccountReplicationType = input (match args.Replication with | LRS -> "LRS"),
                               EnableHttpsTrafficOnly = input args.HttpsOnly,
                               Tags = inputMap args.Tags)) |>
        fun saa -> Account(args.Name,
                           saa,
                           CustomResourceOptions(AdditionalSecretOutputs = List<string>([
                               "PrimaryAccessKey"
                               "SecondaryAccessKey"
                               "PrimaryConnectionString"
                               "PrimaryBlobConnectionString"
                               "SecondaryConnectionString"
                               "SecondaryBlobConnectionString"
                           ])))

    [<CustomOperation("name")>]
    member __.Name(args : StorageAccountArgsRecord, name) = { args with Name = name }

    [<CustomOperation("region")>]
    member __.Region(args : StorageAccountArgsRecord, region) = { args with Region = region }
    
    [<CustomOperation("replication")>]
    member __.Replication(args : StorageAccountArgsRecord, replication) = { args with Replication = replication }
    
    [<CustomOperation("tier")>]
    member __.Tier(args : StorageAccountArgsRecord, tier) = { args with Tier = tier }
    
    [<CustomOperation("httpsOnly")>]
    member __.HttpsOnly(args : StorageAccountArgsRecord, httpsOnly) = { args with HttpsOnly = httpsOnly }
    
    [<CustomOperation("resourceGroup")>]
    member __.ResourceGroup(args : StorageAccountArgsRecord, resourceGroup) = {
        args with ResourceGroup = Object resourceGroup
    }
    
    [<CustomOperation("resourceGroupName")>]
    member __.ResourceGroupName(args : StorageAccountArgsRecord, resourceGroup) = {
        args with ResourceGroup = Name resourceGroup
    }
    
    [<CustomOperation("iotags")>]
    member __.IoTags(args : StorageAccountArgsRecord, tags) = { args with Tags = tags }

    [<CustomOperation("tags")>]
    member __.Tags(args : StorageAccountArgsRecord, tags) = { args with Tags = tags |>
                                                                               List.map (fun (n, v) -> (n, input v)) }

let storageAccount = StorageAccountBuilder()