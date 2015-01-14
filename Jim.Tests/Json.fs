module Jim.Tests.Json

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net.Http
open System.Text

open Xunit
open FsUnit.Xunit

open Jim.Json

let run_with' = run_with default_config

type Foo =
    { foo : string; }

type Bar =
    { bar : string; }

[<Fact>]
let ``Should map JSON from one class to another``() =  
    let mappingFunc (a:Foo) = 
        async {
            return { bar = a.foo }
        }

    let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("{\"foo\":\"foo\"}"))

    (run_with' (mapJsonAsync mappingFunc)) |> req HttpMethod.POST "/" (Some <| postData)
    |> should equal "{\"bar\":\"foo\"}"