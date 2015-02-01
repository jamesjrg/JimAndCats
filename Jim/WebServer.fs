module Jim.WebServer

open Jim
open Jim.AppSettings
open MicroCQRS.Common.CommandAgent

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

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = Successful.OK "Hello"

let webApp postCommand repository =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/users" >>= request (fun r -> QueryEndpoints.listUsers repository)
        url_scan_guid "/users/%s" (fun id -> QueryEndpoints.getUser repository id)
        url "/" >>= index ]
    POST >>= choose [
        url "/users/create" >>= tryMapJson (CommandEndpoints.createUser postCommand)
        url_scan_guid "/users/%s/authenticate" (fun id -> tryMapJson <| QueryEndpoints.authenticate repository id) ]
    PUT >>= choose [ 
        url_scan_guid "/users/%s/name" (fun id -> tryMapJson <| CommandEndpoints.setName postCommand id)
        url_scan_guid "/users/%s/email" (fun id -> tryMapJson <| CommandEndpoints.setEmail postCommand id)
        url_scan_guid "/users/%s/password"  (fun id -> tryMapJson <| CommandEndpoints.setPassword postCommand id) ]

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    let web_config = makeConfig appSettings.Port (Suave.SuaveAdapter(Logging.logary.GetLogger "suave"))
    printfn "Starting JIM on %d" appSettings.Port

    try     
        let postCommand, repository = CommandEndpoints.getCommandPosterAndRepository()
        web_server web_config (webApp postCommand repository)        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    Logging.logary.Dispose()
    0

