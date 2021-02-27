module Pulumi.FSharp.Output

open System.Collections.Generic
open Pulumi

(* TODO: ComponentResourceBuilder
componentResource {
    functionApp { ... }
    plan { ... }
    output [ ... ]
} *)

(* TODO: Output.map
module Output =
    let map (func : 'a -> 'b) (o : Pulumi.Output<'a>) =
        o.Apply func
    
    ...
 *)
 
(* TODO: Add Bind for Input
output {
    let! inp = Input.op_Implicit("test")
    
    return inp
} *)

(* TODO: Add support for parent (_)
resource { parent p }
*)

(* TODO: Await resource creation (let! _ = resource.Urn ?)
output {
    do! resource
} *)

(* TODO: match!
output {
    return match! acme with // acme : Output<'a>
           | Some acme -> acme.AccountKey.ToPem()
           | None      -> "Unavailable in preview, run up to generate"
} *)

(* TODO: Can Output (user-create) be set to: Not available yet (isKnown = false)? No... Pulumi resolves always Tasks
secretOutput {
    return match acme with
           // Ask on GitHub to get a Output.Create(Task) that resolves the task only in up and not when IsDryRun
           | None      -> "Unavailable in preview, run up to generate"
} *)

(* TODO: Ask on GitHub to get pulumi preview/up --show-secrets for debugging *)

(* TODO: Helper for previous stack outputs
let! stackOutputs =
    StackReference(Deployment.Instance.StackName).Outputs
    
let asyncContext = 
    match stackOutputs.TryGetValue "LetsEncryptAccountKey" with
    | true, (:? string as pem) -> found
    | _                        -> notfound *)

(* TODO: Support return! in OutputBuilder *)

(* TODO: Add let! also for Async in OutputBuilder (not only Task, but use Async.StartAsTask) *)

(* TODO: Ignore Output value
    output {
        let! url = apiManagement.GatewayUrl
        
        blob {
            source { Text = url + "/api" }.ToPulumiType
        }
        
        return 0 // We don't need this!
    } *)

module Output =
    let map (func : 'a -> 'b) (o : Pulumi.Output<'a>) =
        o.Apply func
        
    let create (value : 'a) =
        Output.Create<'a>(value)
        
    let createSecret (value : 'a) =
        Output.CreateSecret<'a>(value)
        
    let unsecret<'a> (source : IDictionary<string, obj>) =
        source |>
        Seq.map (fun (kvp) -> kvp.Key, match kvp.Value with
                                       | :? Output<'a> as y -> Output.Unsecret<'a>(y) :> obj
                                       | x                  -> x) |>
        dict