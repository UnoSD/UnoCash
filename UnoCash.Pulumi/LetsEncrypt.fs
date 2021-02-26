module LetsEncrypt

open System.Threading.Tasks
open Certes.Acme.Resource
open DnsClient.Protocol
open Certes.Acme
open System.IO
open DnsClient
open Certes

type CertificateOrderInfo = {
    TxtRecordName: string
    TxtRecordValue: string
    DnsChallenge: IChallengeContext
    Order: IOrderContext
}

let startCertificateOrder (dns : string) (acme : AcmeContext) = async {
    let! order =
        acme.NewOrder([| dns |]) |>
        Async.AwaitTask
        
    let! authzColl =
        order.Authorizations() |>
        Async.AwaitTask
        
    let authz =
        authzColl |> Seq.exactlyOne
        
    let! dnsChallenge =
        authz.Dns() |>
        Async.AwaitTask
    
    let dnsTxt =
        acme.AccountKey.DnsTxt(dnsChallenge.Token)
     
    let recordName =
        if dns.StartsWith("*.") then
            $"_acme-challenge.{dns.Substring(2)}"
        else
            dns
            
    return
        {
            TxtRecordName = recordName
            TxtRecordValue = dnsTxt
            DnsChallenge = dnsChallenge
            Order = order
        }
}

let completeCertificateOrder dns txtRecordInfo = async {
    let rec waitForPropagation () = async {
        if LookupClient().Query(txtRecordInfo.TxtRecordName, QueryType.TXT)
                         .Answers |>
           Seq.filter (fun x -> x :? TxtRecord) |>
           Seq.cast<TxtRecord> |>
           Seq.exists (fun txtRecord -> txtRecord.Text |> Seq.contains(txtRecordInfo.TxtRecordValue)) |>
           not then
            do! Async.Sleep(500)
            do! waitForPropagation()
    }
    
    do! waitForPropagation()
    
    let rec validationResult (challenge : Task<Challenge>) = async {
        let! result =
            challenge |>
            Async.AwaitTask
         
        match result.Status |> Option.ofNullable with
        | Some ChallengeStatus.Pending
        | Some ChallengeStatus.Processing -> do!     Async.Sleep(500)
                                             return! txtRecordInfo.DnsChallenge.Resource() |> validationResult
        | Some ChallengeStatus.Valid      -> return  result.Status.Value
        | _                               -> let error = if result.Error = null then "" else $", {result.Error.Detail}"
                                             return failwith $"Unexpected status {result.Status}{error}"
    }
           
    do! txtRecordInfo.DnsChallenge.Validate() |>
        validationResult |>
        Async.Ignore
   
    let privateKey =
        KeyFactory.NewKey(KeyAlgorithm.ES256)
    
    return! txtRecordInfo.Order.Generate(CsrInfo(CommonName = dns), privateKey) |>
            Async.AwaitTask |>
            Async.map (fun cert -> cert.ToPem())
}

let certificateOrder (dns : string) addRecord (acme : AcmeContext) = async {
    let! txtRecordInfo =
        startCertificateOrder dns acme
    
    addRecord txtRecordInfo.TxtRecordName txtRecordInfo.TxtRecordValue
    
    let! pem =
        completeCertificateOrder dns txtRecordInfo
    
    return pem
}

let createAccount server (email : string) = async {
    let acme =
        AcmeContext(server)
    
    do! acme.NewAccount(email, true) |>
        Async.AwaitTask |>
        Async.Ignore
        
    return acme
}

let saveAccountToFile server email filePath = async {
    let! acme =
        createAccount email server
    
    let pemKey =
        acme.AccountKey.ToPem()
    
    File.WriteAllText(filePath, pemKey)
    
    return acme
}

let loadAccount server pemKey = async {
    let accountKey =
        pemKey |>
        KeyFactory.FromPem
    
    let acme =
        AcmeContext(server, accountKey)
    
    do! acme.Account() |> Async.AwaitTask |> Async.Ignore
    
    return acme
}

let loadAccountFromFile server filePath =
    File.ReadAllText(filePath) |>
    loadAccount server