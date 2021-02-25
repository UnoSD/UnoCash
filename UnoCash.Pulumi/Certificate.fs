module Pulumi.LetsEncrypt.Certificate

open Pulumi.FSharp.Azure.Dns.Inputs
open Pulumi.FSharp.Azure.Dns
open Pulumi.FSharp.Output
open LetsEncrypt
open Pulumi
open Certes

type LetsEncryptCertificateArgs() =
    inherit ResourceArgs()
    
    [<Input("Dns")>]
    member val Dns = Unchecked.defaultof<Input<string>> with get,set

    [<Input("AcmeContext")>]
    member val AcmeContext = Unchecked.defaultof<Input<AcmeContext>> with get,set

type LetsEncryptCertificate(name, args : LetsEncryptCertificateArgs) =
    inherit ComponentResource("unosd:certificates:Acme", name, args)
    
    let mutable pemOutput = Unchecked.defaultof<Output<string>>
    
    do
        pemOutput <- output {
            let! dns = args.Dns.ToOutput()
            
            let! acme = args.AcmeContext.ToOutput()
            
            let! info = startCertificateOrder dns acme |> Async.StartAsTask
            
            let record =
                txtRecord {
                    zoneName "unocash"
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
    member this.Pem with get() = pemOutput