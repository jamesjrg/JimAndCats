module Cats.WebServer

open Cats.Domain
open Cats.Logging

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Types
open Suave.Utils
open Suave.Web
open Suave.Extensions.Json

open Logary
open Logary.Suave

open System
open System.IO

let web_config =
    { default_config with
        mime_types_map = mimeTypesWithJson
        logger = SuaveAdapter(logary.GetLogger "suave")
    }

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = OK "Hello"

let webApp postCommand =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/" >>= index ]  

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting CATS"    
    try        
        web_server web_config (webApp <| fun x -> ())       
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0

