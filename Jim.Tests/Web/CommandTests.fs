module Jim.Tests.Web.CommandTests

open Fuchu
open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.Tests.Web.CreateWebServer
open Jim.WebServer
open MicroCQRS.Common.Testing.SuaveHelpers
open NodaTime
open Suave.Types
open Suave.Testing
open System
open System.Net
open System.Text
open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

let userHasBeenCreated = [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="; CreationTime = epoch} ]

let getWebServerWithNoEvents() = getWebServer []
let getWebServerWithAUser() = getWebServer userHasBeenCreated

[<Tests>]
let commandTests =
    testList "Command web API tests"
        [
        testCase "Should be able to create a user" (fun () ->
            let content, statusCode = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.POST "/users/create" """{"name":"Frank Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}""" statusCodeAndContent

            test <@ content.Contains("\"Id\":") && statusCode = HttpStatusCode.OK @>)

        testCase "Attempting to create user with too short a username returns bad request" (fun () ->
            let actualContent, actualStatusCode = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.POST "/users/create" """{"name":"Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}""" statusCodeAndContent

            test <@ actualContent.Contains("Username must be at least") && actualStatusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to rename a user" (fun () ->
            let actual = requestResponseWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Frank Moss"}""" status_code

            test <@ actual = HttpStatusCode.OK @>)

        testCase "Should not be able to change name to invalid username" (fun () ->
            let content, statusCode = requestResponseWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Bob"}""" statusCodeAndContent

            test <@ content.Contains("Username must be at least") && statusCode = HttpStatusCode.BadRequest @>)

        testCase "Should be able to change email address" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email"  """{"email":"frank@itv.com"}"""

            test <@ actual.Contains("frank@itv.com") && actual.Contains("Email changed") @>)

        testCase "Should not be able to change email to invalid address" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email"  """{"email":"frankitv.com"}"""

            test <@ actual.Contains("Invalid email") @>)

        testCase "Should be able to change password" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password" """{"password":"n3wp4ss"}"""

            test <@ actual.Contains("Password changed") @>)

        testCase "Should not be able to change password to something too short" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password"  """{"password":"p4ss"}"""

            test <@ actual.Contains("Password must be") @>)

        testCase "Should get 404 trying to set name on non-existent user" (fun () ->
            let actual = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" """{"name":"Frank Moss"}""" status_code

            HttpStatusCode.NotFound =? actual)

        testCase "Should get 404 trying to set email of non-existent user" (fun () ->
            let actual = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email" """{"email":"a@b.com"}""" status_code

            HttpStatusCode.NotFound =? actual)

        testCase "Should get 404 trying to set password of non-existent user" (fun () ->
            let actual = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password"  """{"password":"n3wp4ss"}""" status_code

            HttpStatusCode.NotFound =? actual)
        ]