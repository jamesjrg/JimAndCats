module TestingHelpers.SuaveHelpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

let private createPostData (str:string) =
    Some (new ByteArrayContent(Encoding.UTF8.GetBytes(str)))

let private requestResponse methd resource data fResult =
    reqResp methd resource "" data None DecompressionMethods.None id fResult

let runWithDefaultConfig = runWith defaultConfig

let statusCodeAndContent response =
    contentString response, statusCode response

let runWith (getWebServer: unit -> Types.WebPart) methd resource postData fResult = 
    runWithDefaultConfig (getWebServer()) |> requestResponse methd resource postData fResult

let get (getWebServer: unit -> Types.WebPart) resource fResult =
    runWith getWebServer HttpMethod.GET resource None fResult

let post (getWebServer: unit -> Types.WebPart) resource postDataString fResult =
    let postData = createPostData postDataString
    runWith getWebServer HttpMethod.POST resource postData fResult

let put (getWebServer: unit -> Types.WebPart) resource postDataString fResult =
    let postData = createPostData postDataString
    runWith getWebServer HttpMethod.PUT resource postData fResult

