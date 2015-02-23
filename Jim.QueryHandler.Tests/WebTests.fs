module Jim.QueryHandler.Tests.WebTests

open Fuchu
open Jim.Domain
open Jim.QueryHandler.WebServer
open Jim.UserRepository
open NodaTime
open Suave.Testing
open System
open System.Net
open TestingHelpers.SuaveHelpers

open Swensen.Unquote.Assertions

let guid1 = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let epoch = new Instant(0L)

let bobEmail = "bob.holness@itv.com"
let bobPasswordHash = "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="

let getWebServerWithNoEvents() = webApp (new InMemoryUserRepository())

let getWebServerWithAUser() =
    let repo = new InMemoryUserRepository() :> IUserRepository
    repo.Put({User.Id=guid1; Name=Username "Bob Holness"; Email = EmailAddress bobEmail; PasswordHash=PasswordHash bobPasswordHash; CreationTime = epoch }) |> ignore
    webApp repo

[<Tests>]
let queryTests =
    testList "Query web API tests"
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
