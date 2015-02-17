module Suave.Extensions.ConfigDefaults

open System
open Suave
open Suave.Http
open Suave.Web

let mimeTypesWithJson =
    Suave.Http.Writers.defaultMimeTypesMap
        >=> (function
        | ".json" -> Http.Writers.mkMimeType "application/json" true
        | _ -> None)

let makeConfig port logger =
    { defaultConfig with
        mimeTypesMap = mimeTypesWithJson
        logger = logger
        bindings =
            [ {
                scheme  = Types.HTTP
                socketBinding = Sockets.SocketBinding.mk Net.IPAddress.Loopback (uint16 port)
            } ]
    }