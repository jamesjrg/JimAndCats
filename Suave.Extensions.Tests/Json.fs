module Suave.Extensions.Tests.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net
open System.Net.Http
open System.Text

open Suave.Extensions.Json

open Fuchu
open Swensen.Unquote.Assertions

let runWithDefaultConfig = runWith defaultConfig

let reqResp_with_defaults methd resource data f_result =
    reqResp methd resource "" data None DecompressionMethods.None id f_result

type Foo = { foo : string; }

type Bar = { bar : string; }

[<Tests>]
let tests =
    let mappingFunc (a:Foo) = jsonOK { bar = a.foo }

    testList "Json tests"
        [
            testCase "Should map JSON from one class to another" (fun () ->
            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":"foo"}"""))
            let response_data =
                (runWithDefaultConfig (tryMapJson mappingFunc))
                |> req HttpMethod.POST "/" (Some postData)

            """{"bar":"foo"}""" =? response_data)

            testCase "Should return bad request for ill-formatted JSON" (fun () ->
            let badPostData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":foo"}"""))
            let actualStatusCode =
                (runWithDefaultConfig (tryMapJson mappingFunc))
                |> reqResp_with_defaults HttpMethod.POST "/" (Some badPostData) statusCode

            HttpStatusCode.BadRequest =? actualStatusCode)
        ]