module Cats.Tests.Domain.CommandTests

open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Fuchu
open MicroCQRS.Common
open MicroCQRS.Common.Result
open MicroCQRS.Common.CommandFailure
open MicroCQRS.Common.Testing.BDDHelpers
open NodaTime
open System

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let guid1 = createGuid1()

let createEpoch () = new Instant(0L)
let epoch = createEpoch()

let catHasBeenCreated = [CatCreated { Id = guid1; Title=PageTitle "My lovely crowdfunding ask template"; CreationTime = epoch }]

let Expect: Result<Event,CommandFailure> -> Event list * Command -> unit = Expect' (fun () -> new SimpleInMemoryRepository<Cat>()) handleEvent (handleCommand createGuid1 createEpoch)
let ExpectBadRequest = Expect (Failure (BadRequest "any string will do"))
let ExpectSuccess event = Expect (Success event)

[<Tests>]
let tests =
    testList "Domain tests"
        [
            testCase "Should be able to create a CAT" (fun () ->            
                Given []
                |> When ( CreateCat { Title="My lovely crowdfunding ask template" } )
                |> ExpectSuccess (CatCreated { Id = guid1; CreationTime = epoch; Title=PageTitle"My lovely crowdfunding ask template" } ))

            testCase "Should not be able to create a CAT with too short a title" (fun () ->            
                Given []
                |> When ( CreateCat { Title="a" } )
                |> ExpectBadRequest)

            testCase "Should not be able to create a CAT with large whitespace title" (fun () ->            
                Given []
                |> When ( CreateCat { Title="                 "; } )
                |> ExpectBadRequest)

            testCase "Should be able to retitle a CAT" (fun () ->
                Given catHasBeenCreated
                |> When ( SetTitle { Id = guid1; Title="My lovely new cat name"; } )
                |> ExpectSuccess (TitleChanged { Id = guid1; Title=PageTitle "My lovely new cat name"; } ))

            testCase "Should not be able to change title to something too short" (fun () ->            
                Given catHasBeenCreated
                |> When ( SetTitle { Id = guid1; Title="a"; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change title to whitespace" (fun () ->            
                Given catHasBeenCreated
                |> When ( SetTitle { Id = guid1; Title="                   "; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change title of non-existent CAT" (fun () ->
                Given []
                |> When ( SetTitle { Id = guid1; Title="My lovely new cat name"; } )
                |> Expect (Failure NotFound))
        ]