module LetsEncrypt

open System.Threading.Tasks
open Certes.Acme.Resource
open DnsClient.Protocol
open System.IO
open DnsClient
open Certes

let private map f async' =
    async.Bind(async', f >> async.Return)
    
let certificateOrder (dns : string) addRecord (acme : AcmeContext) = async {
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
    
    addRecord recordName dnsTxt
    
    let rec waitForPropagation () = async {
        if LookupClient().Query(recordName, QueryType.TXT)
                         .Answers |>
           Seq.cast<TxtRecord> |>
           Seq.exists (fun txtRecord -> txtRecord.Text |> Seq.contains(dnsTxt)) |>
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
                                             return! dnsChallenge.Resource() |> validationResult
        | Some ChallengeStatus.Valid      -> return  result.Status.Value
        | _                               -> let error = if result.Error = null then "" else $", {result.Error.Detail}"
                                             return failwith $"Unexpected status {result.Status}{error}"
    }
           
    do! dnsChallenge.Validate() |>
        validationResult |>
        Async.Ignore
   
    let privateKey =
        KeyFactory.NewKey(KeyAlgorithm.ES256)
    
    return! order.Generate(CsrInfo(CommonName = dns), privateKey) |>
            Async.AwaitTask |>
            map (fun cert -> cert.ToPem())
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