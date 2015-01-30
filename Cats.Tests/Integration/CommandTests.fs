module Cats.Tests.Integration.CommandTests

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
let commandTests =
    testList "Command integration tests"
        [
        testCase "Should be able to create a cat" (fun () ->
            let content, statusCode = requestResponseWithPostData [] HttpMethod.POST "/cats/create" """{"title":"My lovely cat"}""" statusCodeAndContent

            test <@ content.Contains("\"Id\":") && statusCode = HttpStatusCode.OK @>)

        testCase "Creating a cat with too short a name returns bad request" (fun () ->
            let actualContent, actualStatusCode = requestResponseWithPostData [] HttpMethod.POST "/cats/create" """{"title":"a"}""" statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change title" (fun () ->
            let actual = requestResponseWithPostData catHasBeenCreated HttpMethod.PUT "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" status_code

            test <@ actual = HttpStatusCode.OK @>)

        testCase "Should not be able to change title to something too short" (fun () ->
            let actualContent, actualStatusCode = requestResponseWithPostData catHasBeenCreated HttpMethod.PUT "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"a"}""" statusCodeAndContent

            test <@ actualContent.Contains("Title must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should get 404 trying to set title of non-existent cat" (fun () ->
            let actual = requestResponseWithPostData [] HttpMethod.PUT "/cats/3C71C09A-2902-4682-B8AB-663432C8867B/title" """{"title":"My new lovely cat name"}""" status_code

            HttpStatusCode.NotFound =? actual)
        ]