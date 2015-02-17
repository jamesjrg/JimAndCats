module Cats.Tests.Web.CommandTests

open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.Tests.Web.CreateWebServer
open Fuchu
open EventStore.YetAnotherClient.Testing.SuaveHelpers
open NodaTime
open Suave.Testing
open Suave.Types
open System
open System.Text
open System.Net
open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let ownerGuid1 = new Guid("9F2FFD7A-7B24-4B72-A4A5-8EF507306038")
let epoch = new Instant(0L)
let catHasBeenCreated = [CatCreated {Id = guid1; Title = PageTitle "My lovely cat"; Owner=ownerGuid1; CreationTime=epoch}]

let getWebServerWithNoEvents() = getWebServer []
let getWebServerWithACat() = getWebServer catHasBeenCreated

[<Tests>]
let commandTests =
    testList "Command web API tests"
        [
        testCase "Should be able to create a cat" (fun () ->
            let content, statusCode = post getWebServerWithNoEvents "/cats/create" """{"title":"My lovely cat"}""" None statusCodeAndContent

            test <@ content.Contains("\"Id\":") && statusCode = HttpStatusCode.Created @>)

        testCase "Creating a cat with too short a name returns bad request" (fun () ->
            let actualContent, actualStatusCode = post getWebServerWithNoEvents "/cats/create" """{"title":"a"}""" None statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change title" (fun () ->
            let actual = put getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" None status_code

            test <@ actual = HttpStatusCode.OK @>)

        testCase "Should not be able to change title to something too short" (fun () ->
            let actualContent, actualStatusCode = put getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"a"}""" None statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should get 404 trying to set title of non-existent cat" (fun () ->
            let actual = put getWebServerWithNoEvents "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" None status_code

            HttpStatusCode.NotFound =? actual)
        ]