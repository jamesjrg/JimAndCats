module Suave.Extensions.Tests.Guids

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System
open System.Net
open System.Net.Http
open System.Text

open Suave.Extensions.Guids

open Fuchu
open Swensen.Unquote.Assertions

let runWithDefaultConfig = runWith defaultConfig

let reqResp_with_defaults methd resource data f_result =
    reqResp methd resource "" data None DecompressionMethods.None id f_result

let actualGuid = Guid.NewGuid().ToString()
let webPart = urlScanGuid "/lego/%s/bricks" (fun guid -> Successful.OK (guid.ToString()))
let legoUrl guid = sprintf "/lego/%s/bricks" guid

[<Tests>]
let tests =
    testList "GUID tests"
        [
            testCase "Should work with valid GUID embedded in path" (fun () ->
            let response_data =
                (runWithDefaultConfig webPart)
                |> req HttpMethod.GET (legoUrl actualGuid) None

            actualGuid =? response_data)

            testCase "Should return bad request for ill-formatted GUID embedded in path" (fun () ->
            let actualStatusCode =
                (runWithDefaultConfig webPart)
                |> reqResp_with_defaults HttpMethod.GET (legoUrl "79hd2uwh277892") None statusCode

            HttpStatusCode.BadRequest =? actualStatusCode)
        ]