﻿module Jim.WebServer

open Jim.ApplicationService
open Jim.DataContracts
open Jim.Logging

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Types
open Suave.Utils
open Suave.Json
open Suave.Web

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
let login (appService : AppService) = OK "Hello"
let logout = OK "Hello"

let createUser (appService : AppService) =
    let mappingFunc (createUser:CreateUser) = 
        //TODO handle async response
        let createResponse = appService.createUser(createUser.name, createUser.email, createUser.password)
        { JsonResponse.message = "User created: " + createUser.name }
        
    map_json mappingFunc  

let listUsers (appService : AppService) httpContext =
    async {
        let! users = appService.listUsers ()
        let usersAsString = "Users:\n" + ((Seq.map (fun u -> sprintf "%A" u) users) |> String.concat "\n")
        return! OK usersAsString httpContext
    }

let renameUser (appService : AppService) (id:string) =    
    let mappingFunc (changeName:ChangeName) = 
        //TODO handle async response
        let renameResponse = appService.renameUser(Guid.Parse(id), changeName.name)
        { JsonResponse.message = "Name changed to " + changeName.name }

    map_json mappingFunc

let changePassword appService (id:string) httpContext = OK "Hello" httpContext

let webApp (appService : AppService) =
  choose
    [ GET >>= choose
        [ url "/api-docs" >>= swaggerSpec
          url "/users" >>= listUsers appService
          url "/" >>= index
        ]
      POST >>= choose
        [ url "/login" >>= login appService
          url "/users/create" >>= createUser appService
          url "/logout'" >>= logout
        ]
      PUT >>= choose 
        [ url_scan "/users/%s/password" (fun id -> changePassword appService id)
          url_scan "/users/%s/name" (fun id -> renameUser appService id)
        ]
      RequestErrors.NOT_FOUND "404 not found"
    ] 

[<EntryPoint>]
let main argv = 
    printfn "Starting"    
    try        
        web_server web_config (webApp <| new AppService())        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0