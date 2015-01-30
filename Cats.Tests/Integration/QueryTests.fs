module Cats.Tests.Integration.QueryTests

open System
open System.Text
open System.Net

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

let catHasBeenCreated = [CatCreated {Id = guid1; Title = PageTitle "My lovely cat"; CreationTime=epoch}]

[<Tests>]
let queryTests =
    testList "Query integration tests"
        [
        testCase "Should be able to fetch a CAT" (fun () ->
            let actual = requestContentWithGet catHasBeenCreated "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" 

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent CAT" (fun () ->
            let actual = requestResponseWithGet [] "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" status_code

            HttpStatusCode.NotFound =? actual)
        ]
