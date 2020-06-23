module Pulumi.FSharp.CE.ResourceGroup

open Pulumi.Azure.Core
open Pulumi.FSharp
open Pulumi

type Region =
    | WestEurope

let regionName =
    function
    | WestEurope -> "West Europe"

type ResourceGroupArgsRecord = {
    Name: string
    Region: Region
    Tags: (string * Input<string>) list
}

type ResourceGroupBuilder internal () =
    member __.Yield _ = {
        Name = "" // This needs to be an option or mandatory
        Region = WestEurope
        Tags = []
    }

    member __.Run args =
        args.Region |>
        regionName |>
        input |>
        (fun l  -> ResourceGroupArgs(Location = l, Tags = inputMap args.Tags)) |>
        fun rga -> ResourceGroup(args.Name, rga)

    [<CustomOperation("name")>]
    member __.Name(args, name) = { args with Name = name }

    [<CustomOperation("region")>]
    member __.Region(args, region) = { args with Region = region }
    
    [<CustomOperation("iotags")>]
    member __.IoTags(args, tags) = { args with Tags = tags }

    [<CustomOperation("tags")>]
    member __.Tags(args, tags) = { args with Tags = tags |>
                                                    List.map (fun (n, v) -> (n, input v)) }

let resourceGroup = ResourceGroupBuilder()