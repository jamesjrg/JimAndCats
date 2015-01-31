module Cats.Tests.Integration.QueryTests

open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.WebServer
open Cats.Tests.Integration.CreateWebServer
open Fuchu
open MicroCQRS.Common.Testing.SuaveHelpers
open NodaTime
open System
open System.Text
open System.Net
open Suave
open Suave.Testing
open Suave.Types

open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)
let catHasBeenCreated = [CatCreated {Id = guid1; Title = PageTitle "My lovely cat"; CreationTime=epoch}]

let getWebServerWithNoEvents() = getWebServer []
let getWebServerWithACat() = getWebServer catHasBeenCreated

[<Tests>]
let queryTests =
    testList "Query integration tests"
        [
        testCase "Should be able to fetch a CAT" (fun () ->
            let actual = requestContentWithGet getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" 

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent CAT" (fun () ->
            let actual = requestResponseWithGet getWebServerWithNoEvents "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" status_code

            HttpStatusCode.NotFound =? actual)
        ]
