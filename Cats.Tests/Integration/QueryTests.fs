module Cats.Tests.Integration.QueryTests

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

[<Tests>]
let queryTests =
    testList "Query integration tests"
        [
        testCase "Should be able to fetch a CAT" (fun () ->
            let postCommand, repo = getTestCommandPosterAndRepo [CatCreated { Id = guid1; CreationTime = epoch} ]

            let actual = (run_with' (webApp postCommand repo)) |> req HttpMethod.GET "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" None

            """{"Id":"3c71c09a-2902-4682-b8ab-663432c8867b","CreationTime":"1970-01-01T00:00:00Z"}""" =? actual)

        testCase "Should get 404 for non-existent CAT" (fun () ->
            let postCommand, repo = getTestCommandPosterAndRepo []

            let actual_status_code = (run_with' (webApp postCommand repo)) |> req_resp_with_defaults HttpMethod.GET "/cats/3C71C09A-2902-4682-B8AB-663432C8867B" None status_code
            
            HttpStatusCode.NotFound =? actual_status_code)
        ]
