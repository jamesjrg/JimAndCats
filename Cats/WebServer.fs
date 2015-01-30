module Cats.WebServer

open Cats
open Cats.CommandAgent
open Cats.AppSettings

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open Suave.Web
open Suave.Extensions.ConfigDefaults
open Suave.Extensions.Guids
open Suave.Extensions.Json

open Logary

open System
open System.IO

let web_config = makeConfig appSettings.Port (Suave.SuaveAdapter(Logging.logary.GetLogger "suave"))

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = Successful.OK "Hello"

let webApp postCommand repository =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/cats" >>= request (fun r -> QueryEndpoints.listCats repository)
        url_scan_guid "/cats/%s" (fun id -> QueryEndpoints.getCat repository id)
        url "/" >>= index ]
    POST >>= choose [
        url "/cats/create" >>= tryMapJson (CommandEndpoints.createCat postCommand) ]
    PUT >>= choose [ 
        url_scan_guid "/cats/%s/title" (fun id -> tryMapJson <| CommandEndpoints.setTitle postCommand id) ]

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting CATS on %d" appSettings.Port
    try     
        let postCommand, repository = CommandEndpoints.getCommandPosterAndRepository()
        web_server web_config (webApp postCommand repository)        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    Logging.logary.Dispose()
    0

