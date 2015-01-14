module Jim.Tests.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net.Http
open System.Text

open Jim.Json

open Fuchu
open Swensen.Unquote.Assertions

let run_with' = run_with default_config

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
    let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"foo\":\"foo\"}"))

    let testCode () =
        (run_with' (mapJsonAsync mappingFunc)) |> req HttpMethod.POST "/" (Some <| postData) =? "{\"bar\":\"foo\"}"

    testCase "Should map JSON from one class to another" testCode