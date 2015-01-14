module Jim.Tests.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net
open System.Net.Http
open System.Text

open Jim.Json

open Fuchu
open Swensen.Unquote.Assertions

let run_with' = run_with default_config

let reqRespWithDefaults methd resource data mapResponse =
    req_resp methd resource "" data None DecompressionMethods.None id mapResponse

type Foo =
    { foo : string; }

type Bar =
    { bar : string; }

[<Tests>]
let tests =
    let mappingFunc (a:Foo) = 
        async {
            return { bar = a.foo }
        }

    testList "Json tests"
        [
        testCase "Should map JSON from one class to another" (fun () ->
            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":"foo"}"""))
            let responseData = (run_with' (mapJsonAsync mappingFunc)) |> req HttpMethod.POST "/" (Some postData)
            """{"bar":"foo"}""" =? responseData)

        testCase "Should return bad request" (fun () ->
            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":foo"}"""))
            let statusCode = (run_with' (mapJsonAsync mappingFunc)) |> reqRespWithDefaults HttpMethod.POST "/" (Some postData) status_code
            HttpStatusCode.BadRequest =? statusCode)
        ]