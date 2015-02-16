module Jim.QueryHandler.WebServer

open Jim.QueryHandler
open Jim.QueryHandler.AppSettings
open Jim.Hawk
open Suave
open Logary //must be opened after Suave
open Suave.Http
open Suave.Http.Applicatives
open Suave.Types
open Suave.Web
open Suave.Extensions.ConfigDefaults
open Suave.Extensions.Guids
open Suave.Extensions.Json
open System.IO

let swaggerSpec = Files.browse_file' <| Path.Combine("static", "api-docs.json")

let index = Successful.OK "Hello from JIM"

let authenticateWithRepo repository partNeedingAuth =
    Hawk.authenticate'
        (hawkSettings repository)
        (fun err -> RequestErrors.UNAUTHORIZED (err.ToString()))
        (fun (attr, creds, user) -> partNeedingAuth)

let webApp postCommand repository =
    let requireAuth = authenticateWithRepo repository

    choose [
        GET >>= choose [
            url "/api-docs" >>= swaggerSpec
            url "/" >>= index
            url "/users" >>= (requireAuth <| AppService.listUsers repository)
            url_scan_guid "/users/%s" (fun id -> requireAuth <| AppService.getUser repository id) ]

        RequestErrors.NOT_FOUND "404 not found" ] 

[<EntryPoint>]
let main argv = 
    let web_config = makeConfig appSettings.Port (Suave.SuaveAdapter(Logging.logary.GetLogger "suave"))
    printfn "Starting JIM on %d" appSettings.Port

    try     
        let postCommand, repository = Endpoints.getCommandPosterAndRepository()
        web_server web_config (webApp postCommand repository)
    with
    | e -> Logger.fatal (Logging.getCurrentLogger()) (e.ToString())

    Logging.logary.Dispose()
    0

