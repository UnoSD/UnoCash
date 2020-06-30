module Program

open Pulumi.Azure.ApiManagement.Inputs
open Pulumi.Azure.AppService.Inputs
open Pulumi.Azure.ApiManagement
open Pulumi.Azure.AppService
open Pulumi.FSharp.Output
open Pulumi.FSharp.Azure
open System.Diagnostics
open System.Threading
open Pulumi.AzureAD
open Pulumi.FSharp
open System.IO
open System
open Pulumi

let infra () =
    let group =
        resourceGroup {
            name "unocash"
        }

    let storage =
        storageAccount {
            name          "unocashstorage"
            resourceGroup group
        }
        
    let webContainer =
        storageContainer {
            name          "unocashweb"
            account       storage.Name
            containerName "$web"
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
    
    let apiBlob =
        storageBlob {
            name      "unocashapi"
            account   storage
            container buildContainer
            source    (Config().Require("ApiBuild"))
        }
    
    let codeBlobUrl =
        sasToken {
            account storage
            blob    apiBlob
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
        
    let _ =
        Logger("unocashapimlog",
               LoggerArgs(ApiManagementName = io apiManagement.Name,
                          ResourceGroupName = io group.Name,
                          ApplicationInsights = input (LoggerApplicationInsightsArgs(InstrumentationKey = io appInsights.InstrumentationKey))))
        
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
        storageBlob {
            name      ("unocash" + resourceName + "policyblob")
            account   storage
            container buildContainer
            source    (output {
                           let! appId =
                               spaAdApplication.ApplicationId
                           
                           let policyXmlAsset =
                               appIdToPolicyXml appId |>
                               StringAsset :>
                               AssetOrArchive
                               
                           return policyXmlAsset
                       })
        }
    
    let withSas (baseBlobUrl : Output<string>) =
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
    
    let stack =
        StackReference(Deployment.Instance.StackName)
    
    let sasExpirationDate =
        output {
            let! previousOutputs =
                stack.Outputs
            
            return match previousOutputs.TryGetValue sasExpirationOutputName with
                   | true, (:? string as exp) -> match DateTime.TryParse(exp) with
                                                 | true, x when x > DateTime.Now -> x, false // Unchanged
                                                 | _, _                          -> DateTime.Now.AddYears(1), true
                   | _                        -> DateTime.Now.AddYears(1), true
        }
    
    let token =
        secretOutput {
            let! (_, sasChanged) =
                sasExpirationDate
            
            return!
                match sasChanged with
                | true  -> output { let! (exp, _) = sasExpirationDate
                                    
                                    return! sasToken {
                                        account    storage
                                        container  webContainer
                                        duration   {
                                            From = DateTime.Now
                                            To   = exp
                                        }
                                        permission Read
                                    } }
                | false -> output { let! tokenOutput = stack.Outputs
                                    return tokenOutput.[sasTokenOutputName] :?> string }
        }
    
    let swApiPolicyBlobLink =
        output {
            let! sas =
                token
            
            let apiPolicyXml _ =
                let queryString =
                    sas.Substring(1).Split('&') |>
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
        withSas |>
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
        withSas |>
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
        withSas |>
        io

    let indexPolicyXml applicationId =
        String.Format(File.ReadAllText("StaticWebsiteApimGetIndexOperationPolicy.xml"),
                      Config.TenantId,
                      applicationId)
    
    let swApiGetIndexPolicyBlobLink =
        policyBlob "getindex" indexPolicyXml |>
        (fun pb -> pb.Url) |>
        withSas |>
        io
    
    let functionAppCors =
        input (FunctionAppSiteConfigCorsArgs(AllowedOrigins = inputList [ io apiManagement.GatewayUrl ],
                                             SupportCredentials = input true))
    
    let app =
        FunctionApp("unocashapp",
                    FunctionAppArgs(ResourceGroupName = io group.Name,
                                    AppServicePlanId = io functionPlan.Id,
                                    AppSettings = inputMap [ "runtime", input "dotnet"
                                                             "WEBSITE_RUN_FROM_PACKAGE", io codeBlobUrl
                                                             "APPINSIGHTS_INSTRUMENTATIONKEY", io appInsights.InstrumentationKey
                                                             "StorageAccountConnectionString", io storage.PrimaryConnectionString
                                                             "FormRecognizerKey", input ""
                                                             "FormRecognizerEndpoint", input "" ],
                                    StorageAccountName = io storage.Name,
                                    StorageAccountAccessKey = io storage.PrimaryAccessKey,
                                    Version = input "~3",
                                    SiteConfig = input (FunctionAppSiteConfigArgs(Cors = functionAppCors))))
    
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
    
    let apiOperation (httpMethod : HttpMethod) =
        apiOperation {
            name          ("unocashapimapifunction" + (httpMethod.ToString()))
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
        withSas |>
        io
    
    storageBlob {
        name      "unocashwebconfig"
        blobName  "apibaseurl"
        account   storage
        container webContainer
        blobType  Block
        content   (Config().Require("WebEndpoint") + "/api")
        
    } |> ignore
    
    let sasExpirationDateString =
        output {
            let! (date, _) = sasExpirationDate
            
            return date.ToString("u")
        }
    
    dict [
        "Hostname",                           app.DefaultHostname            :> obj
        "ResourceGroup",                      group.Name                     :> obj
        "StorageAccount",                     storage.Name                   :> obj
        "ApiManagementEndpoint",              apiManagement.GatewayUrl       :> obj
        "ApiManagement",                      apiManagement.Name             :> obj
        "StaticWebsiteApi",                   swApi.Name                       :> obj
        "FunctionApi",                        apiFunction.Name               :> obj
        "ApplicationId",                      spaAdApplication.ApplicationId :> obj
        "FunctionName",                       app.Name                       :> obj
        
        // Outputs to read on next deployment to check for changes
        sasTokenOutputName,                   token                          :> obj
        sasExpirationOutputName,              sasExpirationDateString        :> obj
                               
        // API Management policy files URLs                                        
        "StaticWebsiteApiPolicyLink",         swApiPolicyBlobLink            :> obj
        "StaticWebsiteApiPostPolicyLink",     swApiPostPolicyBlobLink        :> obj
        "StaticWebsiteApiGetPolicyLink",      swApiGetPolicyBlobLink         :> obj
        "StaticWebsiteApiGetIndexPolicyLink", swApiGetIndexPolicyBlobLink    :> obj
        "FunctionApiPolicyLink",              functionApiPolicyBlobLink      :> obj
    ]

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
  
  Deployment.run infra
