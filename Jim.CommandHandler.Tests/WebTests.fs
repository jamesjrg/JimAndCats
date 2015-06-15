module Jim.CommandHandler.Tests.WebTests

open EventStore.YetAnotherClient
open Fuchu
open Jim.CommandHandler
open Jim.CommandHandler.Domain
open NodaTime
open Suave.Testing
open System
open System.Net
open Swensen.Unquote.Assertions
open TestingHelpers.SuaveHelpers

let streamPrefix = "testStream"

let getWebServer events =
    let postCommand, getAggregate, saveEventToNewStream, saveEvents = AppService.getAppServicesForTesting()
    
    if not (List.isEmpty events) then
        saveEvents streamPrefix -1 events |> Async.RunSynchronously
    WebServer.webApp (AppService.createCommandToWebPartMapper postCommand) getAggregate saveEventToNewStream

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

let bob = { UserCreated.Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="; CreationTime = epoch}

let userHasBeenCreated = [UserCreated bob ]

let getWebServerWithNoEvents() = getWebServer []
let getWebServerWithAUser() = getWebServer userHasBeenCreated

[<Tests>]
let commandTests =
    testList "Command web API tests"
        [
        testCase "Should be able to create a user" (fun () ->
            let content, statusCode = post getWebServerWithNoEvents "/users/create" """{"name":"Frank Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}""" statusCodeAndContent

            test <@ content.Contains("\"Id\":") && statusCode = HttpStatusCode.Created @>)

        testCase "Attempting to create user with too short a username returns bad request" (fun () ->
            let actualContent, actualStatusCode = post getWebServerWithNoEvents "/users/create" """{"name":"Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}""" statusCodeAndContent

            test <@ actualContent.Contains("Username must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Attempting to create user with same email as existing user returns bad request" (fun () ->
            let actualContent, actualStatusCode = post getWebServerWithAUser "/users/create" """{"name":"Bob Holness", "email":"bob.holness@itv.com","password":"p4ssw0rd"}""" statusCodeAndContent

            test <@ actualContent.Contains("email") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to rename a user" (fun () ->
            let actual = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Frank Moss"}"""  statusCode

            test <@ actual = HttpStatusCode.OK @>)

        testCase "Should not be able to change name to invalid username" (fun () ->
            let content, statusCode = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Bob"}""" statusCodeAndContent

            test <@ content.Contains("Username must be at least") && statusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change email address" (fun () ->
            let actual = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email"  """{"email":"frank@itv.com"}""" contentString
            test <@ actual.Contains("frank@itv.com") && actual.Contains("Email changed") @>)

        testCase "Should not be able to change email to invalid address" (fun () ->
            let actualContent, actualStatusCode = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email"  """{"email":"frankitv.com"}""" statusCodeAndContent

            test <@ actualContent.Contains("email") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change password" (fun () ->
            let actual = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password" """{"password":"n3wp4ss"}""" contentString

            test <@ actual.Contains("Password changed") @>)

        testCase "Should not be able to change password to something too short" (fun () ->
            let actual = put getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password"  """{"password":"p4ss"}""" contentString

            test <@ actual.Contains("Password must be") @>)

        testCase "Should get unauthorized trying to set name on non-existent user if requester is not an admin" (fun () ->
            let actual = put getWebServerWithNoEvents "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Frank Moss"}""" statusCode

            HttpStatusCode.Unauthorized =? actual)

        testCase "Should get unauthorized trying to set email of non-existent user if requester is not an admin" (fun () ->
            let actual = put getWebServerWithNoEvents "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email" """{"email":"a@b.com"}"""  statusCode

            HttpStatusCode.Unauthorized =? actual)

        testCase "Should get unauthorized trying to set password of non-existent user if requester is not an admin" (fun () ->
            let actual = put getWebServerWithNoEvents "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password"  """{"password":"n3wp4ss"}""" statusCode

            HttpStatusCode.Unauthorized =? actual)

        testCase "Should get 404 for posting to totally incorrect url" (fun () ->
            let actual = post getWebServerWithNoEvents "/flibbles" "flobbles"  statusCode
            
            HttpStatusCode.NotFound =? actual)
        ]

[<Tests>]
let queryTests =
    testList "Web API util query method tests"
        [
        testCase "Should be able to fetch a user" (fun () ->
            let actual = get getWebServerWithAUser "/users/3C71C09A-2902-4682-B8AB-663432C8867B" contentString

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","Name":"Bob Holness","Email":"bob.holness@itv.com","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get unauthorized for non-existent user if requester is not an admin" (fun () ->
            let actual = get getWebServerWithNoEvents "/users/3C71C09A-2902-4682-B8AB-663432C8867B" statusCode
            
            actual =? HttpStatusCode.Unauthorized)

        testCase "Should get 404 for get request to incorrect url" (fun () ->
            let actual = get getWebServerWithNoEvents "/flibbles" statusCode
            
            HttpStatusCode.NotFound =? actual)
        ]