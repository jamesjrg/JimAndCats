module Suave.Json.Tests.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net
open System.Net.Http
open System.Text

open Suave.Json

open Fuchu
open Swensen.Unquote.Assertions


let run_with' = run_with default_config

let req_resp_with_defaults methd resource data f_result =
  req_resp methd resource "" data None DecompressionMethods.None id f_result

type Foo = { foo : string; }

type Bar = { bar : string; }

[<Tests>]
let tests =
    let mappingFunc (a:Foo) = 
        async {
            return { bar = a.foo }
        }

    let mapJsonAsyncPartial =
        mapJsonAsync mappingFunc (fun x -> Successful.OK (serializeObject x))

    testList "Json tests"
        [
            testCase "Should map JSON from one class to another" (fun () ->
            let post_data = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":"foo"}"""))
            let response_data =
                (run_with' mapJsonAsyncPartial)
                |> req HttpMethod.POST "/" (Some post_data)

            """{"bar":"foo"}""" =? response_data)

            testCase "Should return bad request" (fun () ->
            let bad_post_data = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"foo":foo"}"""))
            let actual_status_code =
                (run_with' mapJsonAsyncPartial)
                |> req_resp_with_defaults HttpMethod.POST "/" (Some bad_post_data) status_code

            HttpStatusCode.BadRequest =? actual_status_code)
        ]