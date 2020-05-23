module UnoCash.Fulma.Upload

open Fable.Core
open Fetch
open UnoCash.Fulma.Config

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
