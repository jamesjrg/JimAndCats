module Jim.WebServer

open Jim.App
open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Web
open System.IO

let mime_types =
  Suave.Http.Writers.default_mime_types_map
    >=> (function
    | ".avi" -> Suave.Http.Writers.mk_mime_type "application/json" true
    | _ -> None)

let web_config = { default_config with mime_types_map = mime_types }

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


