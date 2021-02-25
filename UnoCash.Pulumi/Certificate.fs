module Pulumi.LetsEncrypt.Certificate

open Pulumi.FSharp.Azure.Dns.Inputs
open Pulumi.FSharp.Azure.Dns
open Pulumi.FSharp.Output
open LetsEncrypt
open Certes.Acme
open Pulumi

type LetsEncryptCertificateArgs() =
    inherit ResourceArgs()
    
    [<Input("Dns")>]
    member val Dns = Unchecked.defaultof<Input<string>> with get,set

    [<Input("CreateAccount")>]
    member val CreateAccount = Unchecked.defaultof<Input<bool>> with get,set
    
    [<Input("AccountKey")>]
    member val AccountKey = Unchecked.defaultof<Input<string>> with get,set
    
    [<Input("Email")>]
    member val Email = Unchecked.defaultof<Input<string>> with get,set

type LetsEncryptCertificate(name, args : LetsEncryptCertificateArgs) =
    inherit ComponentResource("unosd:certificates:Acme", name, args)
    
    let mutable pemOutput = Unchecked.defaultof<Output<string>>
    let mutable accountKey = Unchecked.defaultof<Output<string>>

    do
        pemOutput <- secretOutput {
            if Deployment.Instance.IsDryRun then
                return null
            else
                // TODO: If inputs unchanged, do not try to do this again
                
                let! create = args.CreateAccount.ToOutput()
                let! email = args.Email.ToOutput()
                let! inputAccountKey = args.AccountKey.ToOutput()
                let! dns = args.Dns.ToOutput()
                
                let! acme =
                    match create, email, inputAccountKey with
                    | true , null , _    -> failwith "Cannot create account without email"
                    | true , email, null -> createAccount WellKnownServers.LetsEncryptStagingV2 email |> Async.StartAsTask
                    | true , _    , _    -> failwith "AccountKey and CreateAccount are mutually exclusive"
                    | false, _    , key  -> loadAccount   WellKnownServers.LetsEncryptStagingV2 key   |> Async.StartAsTask
                
                accountKey <- acme.AccountKey.ToPem() |> Output.createSecret
                
                let! info =
                    startCertificateOrder dns acme |> Async.StartAsTask
                
                let record =
                    txtRecord {
                        // TODO: We should specify the zone name
                        zoneName dns
                        name     info.TxtRecordName
                        
                        records [
                            txtRecordRecord {
                                value info.TxtRecordValue
                            }
                        ]
                    }
                
                // Await the record creation
                let! _ = record.Id
                
                let! pem = completeCertificateOrder dns info |> Async.StartAsTask
                
                return pem
        }
        
        base.RegisterOutputs()
    
    [<Output("Pem")>]
    member this.Pem with get() = pemOutput and set(value) = pemOutput <- value
    
    [<Output("AccountKey")>]
    member this.AccountKey with get() = accountKey and set(value) = accountKey <- value