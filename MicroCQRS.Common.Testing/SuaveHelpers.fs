module MicroCQRS.Common.Testing.SuaveHelpers

open logibit.hawk
open logibit.hawk.Client

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

type HawkTestOptions = 
    { Id: string; Key: string}

let private createPostData (str:string) =
    Some (new ByteArrayContent(Encoding.UTF8.GetBytes(str)))

(* copied from logibit hawk code, it's private so you can't reference it *)
let private from_suave_method =
    function
    | HttpMethod.GET -> logibit.hawk.Types.GET
    | HttpMethod.HEAD -> logibit.hawk.Types.HEAD
    | HttpMethod.PUT -> logibit.hawk.Types.PUT
    | HttpMethod.POST -> logibit.hawk.Types.POST
    | HttpMethod.TRACE -> logibit.hawk.Types.TRACE
    | HttpMethod. DELETE -> logibit.hawk.Types.DELETE
    | HttpMethod.PATCH -> logibit.hawk.Types.PATCH
    | HttpMethod.CONNECT -> logibit.hawk.Types.CONNECT
    | HttpMethod.OPTIONS -> logibit.hawk.Types.OPTIONS
    | HttpMethod.OTHER s -> failwithf "method %s not supported" s

let private getHawkRequestTransform resource methd (hawkOpts:HawkTestOptions) (request:HttpRequestMessage) = 
    let creds = {Types.Credentials.id = hawkOpts.Id; Types.Credentials.key=hawkOpts.Key; Types.Credentials.algorithm=Types.SHA256}
    let clientOptions = ClientOptions.mk' creds

    match header' resource (from_suave_method methd) clientOptions with
    | Choice1Of2 data -> set_auth_header request data
    | Choice2Of2 error -> failwith <| error.ToString()

let private requestResponse methd resource data (hawkOpts:HawkTestOptions option) fResult =
    let fRequest =
        match hawkOpts with
        | Some opts -> getHawkRequestTransform resource methd opts
        | None -> id
    req_resp methd resource "" data None DecompressionMethods.None fRequest fResult

let run_with' = run_with default_config

let statusCodeAndContent response =
    content_string response, status_code response

let runWith (getWebServer: unit -> Types.WebPart) methd resource postData hawkOpts  fResult = 
    run_with' (getWebServer()) |> requestResponse methd resource postData hawkOpts fResult

let get (getWebServer: unit -> Types.WebPart) resource hawkOpts fResult =
    runWith getWebServer HttpMethod.GET resource None hawkOpts fResult

let post (getWebServer: unit -> Types.WebPart) resource postDataString hawkOpts fResult =
    let postData = createPostData postDataString
    runWith getWebServer HttpMethod.POST resource postData hawkOpts fResult

let put (getWebServer: unit -> Types.WebPart) resource postDataString hawkOpts fResult =
    let postData = createPostData postDataString
    runWith getWebServer HttpMethod.PUT resource postData hawkOpts fResult

