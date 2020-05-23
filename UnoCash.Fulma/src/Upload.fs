module UnoCash.Fulma.Upload

open Fable.Core.JsInterop
open Fable.Core
open Fetch
open UnoCash.Fulma.Config

let fileUpload file =
    promise {
        let! response =
            Fetch.fetch receiptUploadSasTokenUrl []
        
        let reader = Browser.Dom.FileReader.Create()
        
        let! sasToken =
            response.text()
        
        let blobName =
            file?name
        
        let url =
            sprintf "https://%s.blob.core.windows.net/%s/%s%s" storageAccount receiptStorageContainer blobName sasToken
        
        let request blob contentLength =
            Fetch.fetch url
                        [ Method HttpMethod.PUT
                          requestHeaders [ HttpRequestHeaders.Custom ("x-ms-content-length", contentLength)
                                           HttpRequestHeaders.Custom ("x-ms-blob-type","BlockBlob") ]
                          Body <| U3.Case1 blob ]
        
        // There must be a better way of doing this...
        let never : JS.Promise<Response> = Promise.sleep 1 |> Promise.map (fun _ -> failwith "")
        let mutable promise = never
        
        reader.onload <- (fun evt -> promise <- request evt.target?result evt?total)
        
        reader.readAsArrayBuffer(file)
        
        let! _ = promise
        
        return blobName
    } 
