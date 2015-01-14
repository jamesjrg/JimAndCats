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
let test =
    let mappingFunc (a:Foo) = 
        async {
            return { bar = a.foo }
        }    

    let testWithValidJson () =
        let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"foo\":\"foo\"}"))
        (run_with' (mapJsonAsync mappingFunc)) |> req HttpMethod.POST "/" (Some <| postData) =? "{\"bar\":\"foo\"}"

    let testWithInvalidJson () =
        let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"foo\":foo\"}"))
        let statusCode = (run_with' (mapJsonAsync mappingFunc)) |> reqRespWithDefaults HttpMethod.POST "/" (Some <| postData) status_code
        statusCode =? HttpStatusCode.BadRequest

    testList "Json tests"
        [
        testCase "Should map JSON from one class to another" testWithValidJson
        testCase "Should return bad request" testWithValidJson
        ]