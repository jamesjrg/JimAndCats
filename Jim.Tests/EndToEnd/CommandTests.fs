module Jim.Tests.EndToEnd.CommandTests

open System
open System.Text

open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.CommandApplicationService
open Jim.WebServer

open Jim.Tests.EndToEnd.Helpers

open Suave
open Suave.Types
open Suave.Web
open Suave.Testing
open Fuchu
open Swensen.Unquote.Assertions

[<Tests>]
let commandTests =
    testList "Command integration tests"
        [
        testCase "Should be able to create a user" (fun () ->
            let commandAppService, queryAppService = getTestAppServices []

            let postData = createPostData """{"name":"Frank Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.POST "/users/create" postData

            test <@ actual.Contains("\"Id\":") && actual.Contains("User created") @>)

        testCase "Should not be able to create user with too short a username" (fun () ->
            let commandAppService, queryAppService = getTestAppServices []

            let postData = createPostData """{"name":"Moss", "email":"frank@somewhere.com","password":"p4ssw0rd"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.POST "/users/create" postData

            test <@ actual.Contains("Username must be at least") @>)

        testCase "Should be able to rename a user" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"name":"Frank Moss"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" postData

            test <@ actual.Contains("Frank Moss") && actual.Contains("Name changed") @>)

        testCase "Should not be able to change name to invalid username" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"name":"Bob"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/name" postData

            test <@ actual.Contains("Username") @>)

        testCase "Should be able to change email address" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"email":"frank@itv.com"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email" postData

            test <@ actual.Contains("frank@itv.com") && actual.Contains("Email changed") @>)

        testCase "Should not be able to change email to invalid address" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"email":"frankitv.com"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/email" postData

            test <@ actual.Contains("Invalid email") @>)

        testCase "Should be able to change password" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"password":"n3wp4ss"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password" postData

            test <@ actual.Contains("Password changed") @>)

        testCase "Should not be able to change password to something too short" (fun () ->
            let commandAppService, queryAppService = getTestAppServices [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch} ]

            let postData = createPostData """{"password":"p4ss"}"""

            let actual = (run_with' (webApp commandAppService queryAppService)) |> req HttpMethod.PUT "/users/3C71C09A-2902-4682-B8AB-663432C8867B/password" postData

            test <@ actual.Contains("Password must be") @>)
        ]