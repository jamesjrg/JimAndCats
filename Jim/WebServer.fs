module Jim.WebServer

open Jim.ApplicationService
open Jim.DataContracts

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Types
open Suave.Utils
open Suave.Json
open Suave.Web

open Logary.Suave
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics
open Logary.Logger

open System
open System.IO

let logary =
    withLogary' "Jim" (
      withTargets [
        Console.create Console.empty "console"
        Debugger.create Debugger.empty "debugger"
      ] >>
      withRules [
        Rule.createForTarget "console"
        Rule.createForTarget "debugger"
      ]
    )

let mime_types =
  Suave.Http.Writers.default_mime_types_map
    >=> (function
    | ".json" -> Suave.Http.Writers.mk_mime_type "application/json" true
    | _ -> None)

let web_config =
    { default_config with
        mime_types_map = mime_types
        logger   = SuaveAdapter(logary.GetLogger "suave")
    }

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let login (appService : AppService) httpContext =
    OK "Hello" httpContext

let createUser (appService : AppService) httpContext =
    async {
        appService.createUser ("Bob Holness", "bob.holness@itv.com", "p4ssw0rd")
        return! OK "User created" httpContext
    }    

let listUsers (appService : AppService) httpContext =
    async {
        let! users = appService.listUsers ()
        let usersAsString = "Users:\n" + ((Seq.map (fun u -> sprintf "%A" u) users) |> String.concat "\n")
        return! OK usersAsString httpContext
    }

let index = OK "Hello"

let setPassword appService (id:string) httpContext = OK "Hello" httpContext

let renameUser (appService : AppService) (id:string) httpContext =
    async {
        let mappingFunc (renameRequest:RenameRequest) = 
            appService.renameUser(Guid.Parse(id), renameRequest.name)
            OK ("Name changed to " + renameRequest.name) httpContext

        return! map_json mappingFunc httpContext
    }

let logout = OK "Hello"

let webApp (appService : AppService) =
  choose
    [ GET >>= choose
        [ url "/api-docs" >>= swaggerSpec
          url "/users" >>= listUsers appService
          url "/" >>= index ]
      POST >>= choose
        [ url "/login" >>= login appService
          url "/users/create" >>= createUser appService
          url_scan "/users/%s/password" (fun id -> setPassword appService id)
          url_scan "/users/%s/name" (fun id -> renameUser appService id)
          url "/logout'" >>= logout ]
      RequestErrors.NOT_FOUND "404 not found" ]

[<EntryPoint>]
let main argv = 
    printfn "Starting"    
    try        
        web_server web_config (webApp <| new AppService())        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0