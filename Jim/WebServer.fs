module Jim.WebServer

open Jim.App
open Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Http.Successful
open Suave.Web

let webApp =
  choose
    [ GET >>= choose
        [ url "/login" >>= OK "Hello"
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
    web_server default_config webApp 
    0


