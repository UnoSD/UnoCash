module UnoCash.Fulma.Upload

open Fable.Core
open Fetch
open UnoCash.Fulma.Config
open UnoCash.Fulma.Models
open Thoth.Json
open System

let receiptParse blobName =
    promise {
        let url =
            sprintf "%s?blobName=%s" getReceiptDataUrl blobName
        
        let! receiptData =
            Thoth.Fetch.Fetch.get<_, Receipt> (url, caseStrategy = CamelCase)
        
        (*let! response =
            Fetch.fetch url
                        [ Method HttpMethod.PUT ]
        
        let! json =
            response.text()
            
        let receiptData =
            match Decode.Auto.fromString<Receipt>(json, caseStrategy=CamelCase) with
            | Ok r    -> r
            | Error e -> printfn "Error parsing receipt json: %s" e
                         {
                             Method = ""
                             Payee = ""
                             Amount = ""
                             Date = DateTime.Today
                         }*)
        
        return {
            Date = Option.defaultValue DateTime.Today receiptData.Date
            Tags = []
            Amount = Option.defaultValue 0. receiptData.Amount |> decimal
            Payee = Option.defaultValue String.Empty receiptData.Payee
            Account = initialAccount
            Status = "New"
            Type = "Regular"
            Description = Option.defaultValue String.Empty receiptData.Method
        }
    }


let fileUpload (blob, name, contentLength) =
    promise {
        let! response =
            Fetch.fetch receiptUploadSasTokenUrl []
        
        let! sasToken =
            response.text()
        
        let url =
            sprintf "https://%s.blob.core.windows.net/%s/%s%s" storageAccount
                                                               receiptStorageContainer
                                                               name
                                                               sasToken
        
        let! _ =
            Fetch.fetch url
                        [ Method HttpMethod.PUT
                          requestHeaders [ HttpRequestHeaders.Custom ("x-ms-content-length", contentLength)
                                           HttpRequestHeaders.Custom ("x-ms-blob-type", "BlockBlob") ]
                          Body <| U3.Case1 blob ]
        
        return name
    } 
