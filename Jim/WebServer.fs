module Jim.WebServer

open Jim.ApplicationService

open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Web

open Logary.Suave
open Logary
open Logary.Configuration
open Logary.Targets
open Logary.Metrics
open Logary.Logger

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

let login = OK "Hello"

let createUser (appService : AppService) =
    appService.createUser ()
    OK "User created"

let listUsers (appService : AppService) =
    appService.listUsers ()
    OK "Hello"

let index = OK "Hello"

let setPassword appService = OK "Hello"

let setName appService =  OK "Hello" 

let logout = OK "Hello"

let webApp (appService : AppService) =
  choose
    [ GET >>= choose
        [ url "/api-docs" >>= swaggerSpec
          url "/users" >>= listUsers appService
          url "/" >>= index ]
      POST >>= choose
        [ url "/login" >>= login
          url "/users/create" >>= createUser appService
          url "/password" >>= setPassword appService
          url "/name'" >>= setName appService
          url "/logout'" >>= logout ] ]

[<EntryPoint>]
let main argv = 
    printfn "Starting"    
    try        
        web_server web_config (webApp <| new AppService())        
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    logary.Dispose()
    0