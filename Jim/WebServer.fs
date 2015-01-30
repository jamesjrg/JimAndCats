module Jim.WebServer

open Jim
open Jim.AppSettings
open Jim.CommandAgent

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open Suave.Web
open Suave.Extensions.ConfigDefaults
open Suave.Extensions.Json

open Logary

open System
open System.IO

let web_config = makeConfig appSettings.Port (Suave.SuaveAdapter(Logging.logary.GetLogger "suave"))

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = Successful.OK "Hello"

//TODO: don't use exceptions
let parseId idString =
    match Guid.TryParse(idString) with
    | true, guid -> guid
    | false, _ -> raise (new Exception("Failed to parse id: " + idString))

let parseIdAndMapToResponse f state id =
    tryMapJson (f state (parseId id))

let webApp postCommand repository =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/users" >>= request (fun r -> QueryEndpoints.listUsers repository)
        url_scan "/users/%s" (fun id -> QueryEndpoints.getUser repository (parseId id))
        url "/" >>= index ]
    POST >>= choose [
        url "/users/create" >>= tryMapJson (CommandEndpoints.createUser postCommand)
        url_scan "/users/%s/authenticate" (fun id -> parseIdAndMapToResponse QueryEndpoints.authenticate repository id) ]
    PUT >>= choose [ 
        url_scan "/users/%s/name" (fun id -> parseIdAndMapToResponse CommandEndpoints.setName postCommand id)
        url_scan "/users/%s/email" (fun id -> parseIdAndMapToResponse CommandEndpoints.setEmail postCommand id)
        url_scan "/users/%s/password" (fun id -> parseIdAndMapToResponse CommandEndpoints.setPassword postCommand id) ]

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting JIM on %d" appSettings.Port
    try     
        let postCommand, repository = CommandEndpoints.getCommandPosterAndRepository()
        web_server web_config (webApp postCommand repository)        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    Logging.logary.Dispose()
    0

