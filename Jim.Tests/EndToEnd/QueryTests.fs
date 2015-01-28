module Jim.Tests.EndToEnd.QueryTests

open System
open System.Text
open System.Net

open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.WebServer
open Jim.Tests.EndToEnd.Helpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing
open Fuchu
open Swensen.Unquote.Assertions

[<Tests>]
let queryTests =
    testList "Query integration tests"
        [
        testCase "Should be able to fetch a user" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.GET "/users/3C71C09A-2902-4682-B8AB-663432C8867B" None

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","Name":"Bob Holness","Email":"bob.holness@itv.com","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent user" (fun () ->
            let commandAppService, queryAppService = getTestAppServices []

            let actual_status_code = (run_with' (webApp commandAppService queryAppService)) |> req_resp_with_defaults HttpMethod.GET "/users/3C71C09A-2902-4682-B8AB-663432C8867B" None status_code
            
            HttpStatusCode.NotFound =? actual_status_code)

        testCase "Authentication with a valid password" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="; CreationTime = epoch} ]

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" (createPostData """{"password":"sxjdfls312w3w"}""")

            """{"IsAuthenticated":true}""" =? actual)

        testCase "Authentication with a invalid password" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "128000:rp4MqoM6SelmRHtM8XF87Q==:MCtWeondG9hLIQ7zahxV6JTPSt4="; CreationTime = epoch} ]

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" (createPostData """{"password":"plibbles"}""")

            """{"IsAuthenticated":false}""" =? actual)

        testCase "Authentication for a non-existent user" (fun () ->
            let commandAppService, queryAppService = getTestAppServices []

            let actual_status_code = (run_with' (webApp commandAppService queryAppService)) |> req_resp_with_defaults HttpMethod.POST "/users/3C71C09A-2902-4682-B8AB-663432C8867B/authenticate" (createPostData """{"password":"p4ssw0rd"}""") status_code

            HttpStatusCode.NotFound =? actual_status_code)
        ]
