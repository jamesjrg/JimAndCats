module Jim.Tests.IntegrationTests

open Suave
open Suave.Types
open Suave.Http
open Suave.Web
open Suave.Testing

open System.Net
open System.Net.Http
open System.Text

open Jim.ApplicationService
open Jim.WebServer

open Fuchu
open Swensen.Unquote.Assertions

let run_with' = run_with default_config

type Foo =
    { foo : string; }

type Bar =
    { bar : string; }

[<Tests>]
let tests =
    testList "Integration tests"
        [
        testCase "Should be able to create a user" (fun () ->
            let postData = new ByteArrayContent(Encoding.UTF8.GetBytes("""{"name":"Frank Moss", "email":"frank@somewhere.com","password":"kkk"}"""))

            let actual = (run_with' (webApp <| new AppService())) |> req HttpMethod.POST "/users/create" (Some postData)

            test <@ actual.Contains("\"id\":") && actual.Contains("User created") @>)
        ]