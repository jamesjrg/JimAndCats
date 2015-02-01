module Jim.Tests.Web.QueryTests

open Fuchu
open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.WebServer
open Jim.Tests.Web.CreateWebServer
open MicroCQRS.Common.Testing.SuaveHelpers
open NodaTime
open Suave.Testing
open Suave.Types
open System
open System.Text
open System.Net

open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

let userHasBeenCreated = [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="; CreationTime = epoch} ]

let getWebServerWithNoEvents() = getWebServer []
let getWebServerWithAUser() = getWebServer userHasBeenCreated

[<Tests>]
let queryTests =
    testList "Query web API tests"
        [
        testCase "Should be able to fetch a user" (fun () ->
            let actual = requestContentWithGet (getWebServerWithAUser) "/users/3C71C09A-2902-4682-B8AB-663432C8867B"

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","Name":"Bob Holness","Email":"bob.holness@itv.com","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent user" (fun () ->
            let actual = requestResponseWithGet getWebServerWithNoEvents "/users/3C71C09A-2902-4682-B8AB-663432C8867B" status_code
            
            HttpStatusCode.NotFound =? actual)

        testCase "Authentication with a valid password" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" """{"password":"sxjdfls312w3w"}"""

            """{"IsAuthenticated":true}""" =? actual)

        testCase "Authentication with a invalid password" (fun () ->
            let actual = requestContentWithPostData getWebServerWithAUser HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" """{"password":"plibbles"}"""

            """{"IsAuthenticated":false}""" =? actual)

        testCase "Authentication for a non-existent user" (fun () ->
            let actual = requestResponseWithPostData getWebServerWithNoEvents HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" """{"password":"p4ssw0rd"}""" status_code

            HttpStatusCode.NotFound =? actual)
        ]
