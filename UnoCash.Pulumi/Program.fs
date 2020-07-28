module Program

open Pulumi.Azure.ApiManagement.Inputs
open System.Collections.Generic
open Pulumi.Azure.ApiManagement
open Pulumi.FSharp.Azure.Legacy
open System.Threading.Tasks
open Pulumi.FSharp.Output
open Pulumi.FSharp.Config
open Pulumi.FSharp.Azure
open System.Diagnostics
open System.Threading
open Pulumi.AzureAD
open Pulumi.FSharp
open System.IO
open System
open Pulumi

type ParsedSasToken =
    | Valid of string * DateTime
    | ExpiredOrInvalid
    | Missing

let infra() =
    let group =
        Pulumi.FSharp.Azure.Core.resourceGroup {
            name "unocash"
        }
    
    let storage =
        Pulumi.FSharp.Azure.Storage.account {
            name          "unocashstorage"
            resourceGroup group.Name
            accountReplicationType "LRS"
            accountTier "Standard"
        }
        
    let webContainer =
        Pulumi.FSharp.Azure.Storage.container {
            name               "unocashweb"
            storageAccountName storage.Name
            resourceName      "$web"
        }
            
    let buildContainer =
        storageContainer {
            name    "unocashbuild"
            account storage
        }
    
    let functionPlan =
        appService {
            name          "unocashasp"
            resourceGroup group
            kind          FunctionAppKind
        }
    
    let apiBuildContent =
        config.["ApiBuild"] |>
        File.ReadAllText |>
        StringAsset
    
    let apiBlob =
        storageBlob {
            name      "unocashapi"
            account   storage
            container buildContainer
            source    apiBuildContent
        }
    
    let codeBlobUrl =
        secretOutput {
            return! sasToken {
                        account storage
                        blob    apiBlob
                    }
        }

    let appInsights =
        appInsight {
            name            "unocashai"
            resourceGroup   group
            applicationType Web
            retentionInDays 90
        }
        
    let apiManagement =
        let templateOutputs =
            armTemplate {
                name          "unocashapim"
                resourceGroup group
                jsonFile      "ApiManagement.json"
                parameters    [ "apiManagementServiceName", input "unocashapim"
                                "location", io group.Location ]
            } |>
            fun at -> at.Outputs
        
        {| Name = output { let! outputs = templateOutputs
                           return outputs.["name"] }
           GatewayUrl = output { let! outputs = templateOutputs
                                 return outputs.["gatewayUrl"] } |}
        
    LoggerApplicationInsightsArgs(InstrumentationKey = io appInsights.InstrumentationKey) |>
    fun la -> Logger("unocashapimlog",
                     LoggerArgs(ApiManagementName = io apiManagement.Name,
                                ResourceGroupName = io group.Name,
                                ApplicationInsights = input la)) |> ignore
        
    let webContainerUrl =
        output {
            let! accountName = storage.Name
            let! containerName = webContainer.Name
            
            return sprintf "https://%s.blob.core.windows.net/%s" accountName containerName
        }

    let swApi =
        apimApi {
            name          "unocashapimapi"
            apiName       "staticwebsite"
            resourceGroup group
            apim          apiManagement.Name
            displayName   "StaticWebsite"
            protocol      HttpHttps
            serviceUrl    webContainerUrl
        }

    let spaAdApplication =
        Application("unocashspaaadapp",
                    ApplicationArgs(ReplyUrls = inputList [ io apiManagement.GatewayUrl ],
                                    Oauth2AllowImplicitFlow = input true))
    

    let policyBlob resourceName (appIdToPolicyXml : string -> string) =
        let policyXmlAsset =
            output {
                let! appId =
                    spaAdApplication.ApplicationId
                
                let policyXmlAsset =
                    appIdToPolicyXml appId |>
                    StringAsset :>
                    AssetOrArchive
                    
                return policyXmlAsset
            }
            
        storageBlob {
            name      ("unocash" + resourceName + "policyblob")
            account   storage
            container buildContainer
            source    policyXmlAsset
        }
    
    let withSasToken (baseBlobUrl : Output<string>) =
        secretOutput {
            let! url = baseBlobUrl

            let! sas =
                sasToken {
                    account    storage
                    container  buildContainer
                    duration   {
                        From = DateTime.Now
                        To   = DateTime.Now.AddHours(1.)
                    }
                    permission Read
                }

            return url + sas
        }
        
    let sasExpirationOutputName = "SasTokenExpiration"
    let sasTokenOutputName = "SasToken"
    
    let token =
        secretOutput {
            let! previousOutputs =
                StackReference(Deployment.Instance.StackName).Outputs

            let tokenValidity =
                let getTokenIfValid (expirationString : string) =
                    match DateTime.TryParse expirationString with
                    | true, x when x > DateTime.Now -> Valid (
                                                           previousOutputs.[sasTokenOutputName] :?> string,
                                                           x
                                                       )
                    | _                             -> ExpiredOrInvalid
                
                match previousOutputs.TryGetValue sasExpirationOutputName with
                | true, (:? string as exp) -> getTokenIfValid exp
                | _                        -> Missing
            
            return!
                match tokenValidity with
                | Missing
                | ExpiredOrInvalid      -> let expiry = DateTime.Now.AddYears(1)
                                           sasToken {
                                               account    storage
                                               container  webContainer
                                               duration   {
                                                   From = DateTime.Now
                                                   To   = expiry
                                               }
                                               permission Read
                                           } |> (fun x -> x.Apply(fun y -> (y, expiry )))
                 | Valid (sasToken, e ) -> output { return sasToken, e }
        }
    
    let swApiPolicyBlobLink =
        output {
            let! (tokenValue, _) =
                token
                
            let apiPolicyXml _ =
                let queryString =
                    tokenValue.Substring(1).Split('&') |>
                    Array.map ((fun pair -> pair.Split('=')) >>
                               (fun arr -> (arr.[0], arr.[1]))) |>
                    Map.ofArray
                
                let formatValues =
                    seq {
                        yield Config().Require("WebEndpoint") :> obj
                        
                        for key in ["sv";"sr";"st";"se";"sp";"spr";"sig"] do
                            yield queryString.[key] :> obj
                    } |>
                    Array.ofSeq
                
                String.Format(File.ReadAllText("StaticWebsiteApimApiPolicy.xml"),
                              formatValues)

            let blob =
                policyBlob "mainapi" apiPolicyXml
            
            return! blob.Url
        } |>
        withSasToken |>
        io
    
    apiOperation {
        name          "unocashapimindexoperation"
        resourceGroup group
        apim          apiManagement.Name
        api           swApi
        displayName   "GET index"
    } |> ignore
        
    apiOperation {
        name          "unocashapimoperation"
        resourceGroup group
        apim          apiManagement.Name
        api           swApi
        urlTemplate   "/*"     
    } |> ignore
    
    let getPolicy applicationId =
        String.Format(File.ReadAllText("StaticWebsiteApimGetOperationPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let swApiGetPolicyBlobLink =
        policyBlob "get" getPolicy |>
        (fun pb -> pb.Url) |>
        withSasToken |>
        io
    
    apiOperation {
        name          "unocashapimpostoperation"
        resourceGroup group
        apim          apiManagement.Name
        api           swApi
        method        Post
        displayName   "POST AAD token"
    } |> ignore
    
    let postPolicy applicationId =
        String.Format(File.ReadAllText("StaticWebsiteApimPostOperationPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let swApiPostPolicyBlobLink =
        policyBlob "post" postPolicy |>
        (fun pb -> pb.Url) |>
        withSasToken |>
        io

    let indexPolicyXml applicationId =
        String.Format(File.ReadAllText("StaticWebsiteApimGetIndexOperationPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let swApiGetIndexPolicyBlobLink =
        policyBlob "getindex" indexPolicyXml |>
        (fun pb -> pb.Url) |>
        withSasToken |>
        io
    
    let app =
        functionApp {
            name            "unocashapp"
            resourceGroup   group
            plan            functionPlan
            appSettings     [
                Runtime     Dotnet
                Package     codeBlobUrl
                AppInsight  appInsights
                CustomIO    ("StorageAccountConnectionString", storage.PrimaryConnectionString)
                Custom      ("FormRecognizerKey"             , "")
                Custom      ("FormRecognizerEndpoint"        , "")
            ]               
            storageAccount  storage
            version         "~3"
            allowedOrigin   apiManagement.GatewayUrl
            corsCredentials true
        }
    
    let apiFunction =
        apimApi {
            name          "unocashapimapifunction"
            apiName       "api"
            path          "api"
            resourceGroup group
            apim          apiManagement.Name
            displayName   "API"
            protocol      Https
            serviceUrl    (app.DefaultHostname.Apply (sprintf "https://%s"))
        }
    
    let apiOperation httpMethod =
        apiOperation {
            name          ("unocashapimapifunction" + (httpMethod.ToString().ToLower()))
            resourceGroup group
            apim          apiManagement.Name
            api           apiFunction
            method        httpMethod
            urlTemplate   "/*"
        }
    
    let _ =
        [ Get; Post; HttpMethod.Delete; Put ] |>
        List.map apiOperation
    
    let apiFunctionPolicyXml applicationId =
        String.Format(File.ReadAllText("APIApimApiPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let functionApiPolicyBlobLink =
        policyBlob "functionapi" apiFunctionPolicyXml |>
        (fun pb -> pb.Url) |>
        withSasToken |>
        io
    
    storageBlob {
        name      "unocashwebconfig"
        blobName  "apibaseurl"
        account   storage
        container webContainer
        blobType  Block
        content   (Config().Require("WebEndpoint") + "/api")
        
    } |> ignore

    let sasExpiry =
        output {
            let! (_, expiry) = token            
            return expiry.ToString("u")
        }
    
    dict [
        "Hostname",                           app.DefaultHostname            :> obj
        "ResourceGroup",                      group.Name                     :> obj
        "StorageAccount",                     storage.Name                   :> obj
        "ApiManagementEndpoint",              apiManagement.GatewayUrl       :> obj
        "ApiManagement",                      apiManagement.Name             :> obj
        "StaticWebsiteApi",                   swApi.Name                     :> obj
        "FunctionApi",                        apiFunction.Name               :> obj
        "ApplicationId",                      spaAdApplication.ApplicationId :> obj
        "FunctionName",                       app.Name                       :> obj
        
        // Outputs to read on next deployment to check for changes
        sasTokenOutputName,                   token.Apply fst                :> obj
        sasExpirationOutputName,              sasExpiry                      :> obj
                               
        // API Management policy files URLs                                        
        "StaticWebsiteApiPolicyLink",         swApiPolicyBlobLink            :> obj
        "StaticWebsiteApiPostPolicyLink",     swApiPostPolicyBlobLink        :> obj
        "StaticWebsiteApiGetPolicyLink",      swApiGetPolicyBlobLink         :> obj
        "StaticWebsiteApiGetIndexPolicyLink", swApiGetIndexPolicyBlobLink    :> obj
        "FunctionApiPolicyLink",              functionApiPolicyBlobLink      :> obj
    ]

type bclList<'a> =
    System.Collections.Generic.List<'a>

let ignoreBlobSourceChanges (args : ResourceTransformationArgs) =
    if args.Resource.GetResourceType() = "azure:storage/blob:Blob" then
        args.Options.IgnoreChanges <- bclList(["source"])
    ResourceTransformationResult(args.Args, args.Options) |> Nullable

let stackOptions =
        StackOptions(
            ResourceTransformations =
                bclList([
                    if Environment.GetEnvironmentVariable("AGENT_ID") = null then
                        yield ResourceTransformation(ignoreBlobSourceChanges)
                ]))

[<EntryPoint>]
let main _ =
    let rec waitForDebugger () =
        match Debugger.IsAttached with
        | false -> Thread.Sleep(100)
                   printf "."
                   waitForDebugger ()
        | true  -> printfn " attached"

    match Environment.GetEnvironmentVariable("PULUMI_DEBUG_WAIT") = "1" with
    | true -> printf "Awaiting debugger to attach to the process"
              waitForDebugger ()
    | _    -> ()

    Deployment.RunAsync(Func<Task<IDictionary<string, obj>>>(infra >> Task.FromResult), stackOptions)
              .GetAwaiter()
              .GetResult()