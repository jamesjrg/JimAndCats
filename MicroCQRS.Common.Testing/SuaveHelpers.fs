module MicroCQRS.Common.Testing.SuaveHelpers

open logibit.hawk.Client

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

let run_with' = run_with default_config

let createPostData (str:string) =
    Some (new ByteArrayContent(Encoding.UTF8.GetBytes(str)))

let statusCodeAndContent response =
    content_string response, status_code response

let req_resp_with_defaults methd resource data f_result =
    req_resp methd resource "" data None DecompressionMethods.None id f_result

let requestResponseWithGet (getWebServer: unit -> Types.WebPart) resource fResult =
    run_with' (getWebServer()) |> req_resp_with_defaults HttpMethod.GET resource None fResult

let requestResponseWithPostData (getWebServer: unit -> Types.WebPart) methodType resource postDataString fResult =
    let postData = createPostData postDataString
    run_with' (getWebServer()) |> req_resp_with_defaults methodType resource postData fResult

let requestContentWithPostData (getWebServer: unit -> Types.WebPart) methodType resource postDataString =
    requestResponseWithPostData getWebServer methodType resource postDataString content_string

let requestContentWithGet getWebServer resource =
    requestResponseWithGet getWebServer resource content_string

let x uri meth paramters =
    header' uri meth paramters

let y req headerData = 
    set_auth_header req headerData
