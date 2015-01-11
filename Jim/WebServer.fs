module Jim.WebServer

open Jim.App

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

let webApp =
  choose
    [ GET >>= choose
        [ url "/api-docs" >>= swaggerSpec
          url "/login" >>= OK "Hello"
          url "/users/create" >>= OK "Hello"
          url "/password" >>= OK "Hello"
          url "/name" >>= OK "Hello"
          url "/users" >>= OK "Hello"
          url "/" >>= OK "Hello" ]
      POST >>= choose
        [ url "/login" >>= OK "Hello"
          url "/users/create" >>= OK "Hello"
          url "/password" >>= OK "Hello"
          url "/name'" >>= OK "Hello"
          url "/logout'" >>= OK "Hello" ] ]

[<EntryPoint>]
let main argv = 
    printfn "Starting"
    startService |> ignore
    web_server web_config webApp 
    0


