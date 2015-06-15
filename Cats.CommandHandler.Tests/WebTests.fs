module Cats.CommandHandler.Tests.WebTests

open Cats.CommandHandler
open Cats.CommandHandler.Domain
open Fuchu
open TestingHelpers.SuaveHelpers
open NodaTime
open Suave.Testing
open System
open System.Net
open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let ownerGuid1 = new Guid("9F2FFD7A-7B24-4B72-A4A5-8EF507306038")
let epoch = new Instant(0L)
let catHasBeenCreated = CatCreated {Id = guid1; Title = PageTitle "My lovely cat"; Owner=ownerGuid1; CreationTime=epoch}

let getWebServer initialEvent =
    let postCommand, getAggregate, saveEventToNewStream = AppService.getAppServices()
    
    match initialEvent with
    | Some cat -> saveEventToNewStream guid1 cat |> Async.RunSynchronously
    | _ -> ()

    WebServer.webApp (AppService.createCommandToWebPartMapper postCommand) getAggregate saveEventToNewStream

let getWebServerWithNoEvents() = getWebServer None
let getWebServerWithACat() = getWebServer (Some catHasBeenCreated)

[<Tests>]
let commandTests =
    testList "Command web API tests"
        [
        testCase "Should be able to create a cat" (fun () ->
            let content, statusCode = post getWebServerWithNoEvents "/cats/create" """{"title":"My lovely cat"}""" statusCodeAndContent

            test <@ content.Contains("\"Id\":") && statusCode = HttpStatusCode.Created @>)

        testCase "Creating a cat with too short a name returns bad request" (fun () ->
            let actualContent, actualStatusCode = post getWebServerWithNoEvents "/cats/create" """{"title":"a"}""" statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change title" (fun () ->
            let actual = put getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" statusCode

            test <@ actual = HttpStatusCode.OK @>)

        testCase "Should not be able to change title to something too short" (fun () ->
            let actualContent, actualStatusCode = put getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"a"}""" statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should get 404 trying to set title of non-existent cat" (fun () ->
            let actual = put getWebServerWithNoEvents "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" statusCode

            HttpStatusCode.NotFound =? actual)
        ]

[<Tests>]
let diagnosticQueryTests =
    testList "Diagnostic query web API tests"
        [
        testCase "Should be able to fetch a CAT" (fun () ->
            let actual = get getWebServerWithACat "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" contentString

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent CAT" (fun () ->
            let actual = get getWebServerWithNoEvents "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" statusCode

            HttpStatusCode.NotFound =? actual)
        ]