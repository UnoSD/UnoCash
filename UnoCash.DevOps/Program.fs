open Microsoft.FSharp.Reflection
open FSharp.Data

type ParameterType =
    | String of string option
    | Number of decimal option

let parameter name defaultValue values isSecret =
    let typeName =
        match FSharpValue.GetUnionFields(defaultValue, typeof<ParameterType>) with
        | case, _ -> case.Name.ToLowerInvariant()
    
    let value =
        match defaultValue with
        | String x -> x |> Option.map JsonValue.String
        | Number x -> x |> Option.map JsonValue.Number
        
    
    let defaultProperty =
        value |>
        (function | Some x -> [| "default", x |] | None -> [||]) 
    
    [|
        "name", JsonValue.String name
        "type", JsonValue.String typeName            
        "values", JsonValue.Array (values |> Array.map JsonValue.String)
        "secret", JsonValue.Boolean isSecret
    |] |>
    Array.append defaultProperty |>
    JsonValue.Record

[<EntryPoint>]
let main _ =  
    let pipeline =
        JsonValue.Record [|
            "parameters", JsonValue.Array [|
                parameter "pulumi_backend_resource_group" (Some "Pulumi" |> String) [||] false
                parameter "pulumi_backend_location" (Some "WestEurope" |> String) [||] false
            |]
        |]
        
    pipeline.ToString() |> printfn "%s"
    
    0