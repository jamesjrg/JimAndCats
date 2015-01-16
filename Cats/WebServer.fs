module Cats.WebServer

open Cats.ApplicationService
open Cats.Domain
open Cats.JsonRequests
open Cats.Logging

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Types
open Suave.Utils
open Suave.Web
open Suave.Json

open Logary
open Logary.Suave

open System
open System.IO

let mime_types =
  Suave.Http.Writers.default_mime_types_map
    >=> (function
    | ".json" -> Suave.Http.Writers.mk_mime_type "application/json" true
    | _ -> None)

let web_config =
    { default_config with
        mime_types_map = mime_types
        logger = SuaveAdapter(logary.GetLogger "suave")
    }

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = OK "Hello"

let webApp (appService : AppService) =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/" >>= index ]  

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting CATS"    
    try        
        web_server web_config (webApp <| new AppService())        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0

