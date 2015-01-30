module Suave.Extensions.ConfigDefaults

open System
open Suave
open Suave.Http
open Suave.Web

let mimeTypesWithJson =
    Suave.Http.Writers.default_mime_types_map
        >=> (function
        | ".json" -> Http.Writers.mk_mime_type "application/json" true
        | _ -> None)

let makeConfig port logger =
    { default_config with
        mime_types_map = mimeTypesWithJson
        logger = logger
        bindings =
            [ {
                scheme  = Types.HTTP
                socket_binding = Sockets.SocketBinding.mk Net.IPAddress.Loopback (uint16 port)
            } ]
    }