module Jim.WebServer

open Jim.ApiResponses
open Jim.Domain.CommandsAndEvents
open Jim.CommandApplicationService
open Jim.QueryApplicationService
open Jim.AppServiceInit
open Jim.JsonRequests
open Jim.Logging

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

let web_config =
    { default_config with
        mime_types_map = mimeTypesWithJson
        logger = SuaveAdapter(logary.GetLogger "suave")
    }

let appResponseToWebPart = function
    | OK response -> Successful.OK (serializeObject response)
    | NotFound -> RequestErrors.NOT_FOUND "Not found"
    | BadRequest response -> RequestErrors.BAD_REQUEST (serializeObject response)

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = OK "Hello"

let listUsers (appService : QueryAppService) : Types.WebPart =
    request (fun r ->
        let result = appService.listUsers ()
        appResponseToWebPart result)

let getUser (appService : QueryAppService) (id:Guid) =
    request (fun r ->
        let result = appService.getUser(id)
        appResponseToWebPart result)

let runCommand (appService : CommandAppService) (command:Command) =
    async {
        return! appService.runCommand(command)
    }

let createUser (appService : CommandAppService) (requestDetails:CreateUserRequest) =   
    runCommand appService (CreateUser {Name=requestDetails.name; Email=requestDetails.email; Password=requestDetails.password})

let setName (appService : CommandAppService) (id:Guid) (requestDetails:SetNameRequest) =    
    runCommand appService (SetName{ Id=id; Name = requestDetails.name})

let setEmail (appService : CommandAppService) (id:Guid) (requestDetails:SetEmailRequest) =
    runCommand appService ( SetEmail {Id = id; Email = requestDetails.email} )

let setPassword (appService : CommandAppService) (id:Guid) (requestDetails:SetPasswordRequest) =    
    runCommand appService ( SetPassword{ Id=id; Password = requestDetails.password})

let authenticate (appService : QueryAppService) (id:Guid) (requestDetails:AuthenticateRequest) =
    async {
        return appService.authenticate( {Id=id; Password=requestDetails.password})
    }

//TODO: don't use exceptions
let parseId idString =
    match Guid.TryParse(idString) with
    | true, guid -> guid
    | false, _ -> raise (new Exception("Failed to parse id: " + idString))

let mapResponse f appService =
    mapJsonAsync (f appService) appResponseToWebPart

let parseIdAndMapResponse f appService id =
    mapJsonAsync (f appService (parseId id)) appResponseToWebPart 

let webApp commandAppService queryAppService =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/users" >>= listUsers queryAppService
        url_scan "/users/%s" (fun id -> getUser queryAppService (parseId id))
        url "/" >>= index ]
    POST >>= choose [
        url "/users/create" >>= mapResponse createUser commandAppService
        url_scan "/users/%s/authenticate" (fun id -> parseIdAndMapResponse authenticate queryAppService id) ]
    PUT >>= choose [ 
        url_scan "/users/%s/name" (fun id -> parseIdAndMapResponse setName commandAppService id)
        url_scan "/users/%s/email" (fun id -> parseIdAndMapResponse setEmail commandAppService id)
        url_scan "/users/%s/password" (fun id -> parseIdAndMapResponse setPassword commandAppService id) ]

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting JIM"    
    try     
        let commandAppService, queryAppService = getAppServices()
        web_server web_config (webApp commandAppService queryAppService)        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0

