module Jim.WebServer

open Jim.ApplicationService
open Jim.Domain
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

let listUsers (appService : AppService) httpContext =
    async {
        let! users = appService.listUsers ()
        let usersAsString = "Users:\n" + ((Seq.map (fun u -> sprintf "%A" u) users) |> String.concat "\n")
        return! OK usersAsString httpContext
    }

let createUser (appService : AppService) : Types.WebPart =
    map_json_async (fun (requestDetails:CreateUserRequest) ->
        async {
            return! appService.createUser(
                CreateUser {Name=requestDetails.name; Email=requestDetails.email; Password=requestDetails.password})
        })

let setName (appService : AppService) (id:Guid) (requestDetails:SetNameRequest) =    
    async {
        return! appService.setName(SetName{ Id=id; Name = requestDetails.name})
    }

let setEmail (appService : AppService) (id:Guid) (requestDetails:SetEmailRequest) =
    async {
        return! appService.setEmail( SetEmail {Id = id; Email = requestDetails.email} )
    }

let setPassword (appService : AppService) (id:Guid) (requestDetails:SetPasswordRequest) =    
    async {
        return! appService.setPassword( SetPassword{ Id=id; Password = requestDetails.password})
    }

let authenticate (appService : AppService) (id:Guid) (requestDetails:AuthenticateRequest) =
    async {
        return! appService.authenticate(id, requestDetails)
    }

//TODO: don't use exceptions
let parseId idString =
    match Guid.TryParse(idString) with
    | true, guid -> guid
    | false, _ -> raise (new Exception("Failed to parse id: " + idString))

let parseIdAndMapJsonAsync f (appService : AppService) id =
    map_json_async (f appService (parseId id))

let webApp (appService : AppService) =
  choose [
    GET >>= choose [
        url "/api-docs" >>= swaggerSpec
        url "/users" >>= listUsers appService
        url "/" >>= index ]
    POST >>= choose [
        url "/users/create" >>= createUser appService
        url_scan "/users/%s/authenticate" (fun id -> parseIdAndMapJsonAsync authenticate appService id) ]
    PUT >>= choose [ 
        url_scan "/users/%s/name" (fun id -> parseIdAndMapJsonAsync setName appService id)
        url_scan "/users/%s/email" (fun id -> parseIdAndMapJsonAsync setEmail appService id)
        url_scan "/users/%s/password" (fun id -> parseIdAndMapJsonAsync setPassword appService id) ]

    RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting JIM"    
    try        
        web_server web_config (webApp <| new AppService())        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0

