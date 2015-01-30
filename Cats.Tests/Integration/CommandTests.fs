module Cats.Tests.Integration.CommandTests

open System
open System.Text

open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.WebServer

open Cats.Tests.Integration.Helpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing
open Fuchu
open Swensen.Unquote.Assertions

[<Tests>]
let commandTests =
    testList "Command integration tests"
        [
        testCase "Should be able to create a CAT" (fun () ->
            let postCommand, repo = getTestCommandPosterAndRepo []

            let postData = createPostData """{"name":"Frank Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}"""

            let actual = (run_with' (webApp postCommand repo)) |> req HttpMethod.POST "/cats/create" postData

            test <@ actual.Contains("\"Id\":") && actual.Contains("CAT created") @>)
        ]