module Cats.CommandHandler.Tests.Domain

open Cats.CommandHandler.Domain
open Fuchu
open GenericErrorHandling
open TestingHelpers.BDDHelpers
open NodaTime
open System

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let catGuid1 = createGuid1()
let ownerGuid1 = new Guid("9F2FFD7A-7B24-4B72-A4A5-8EF507306038")

let createEpoch () = new Instant(0L)
let epoch = createEpoch()

[<Tests>]
let creationTests =
    let Expect = ExpectAfterCreateCommand (CommandHandling.createCat createGuid1 createEpoch)
    let ExpectBadRequest = Expect (Failure (BadRequest "any string will do"))

    testList "Cat creation tests"
        [
            testCase "Should be able to create a CAT" (fun () ->   
                { CreateCat.Title="My lovely crowdfunding ask template"; Owner=ownerGuid1 }
                |> Expect (Success { CatCreated.Id = catGuid1; CreationTime = epoch; Title=PageTitle"My lovely crowdfunding ask template"; Owner=ownerGuid1} ))

            testCase "Should not be able to create a CAT with too short a title" (fun () ->            
                { CreateCat.Title="a"; Owner=ownerGuid1 }
                |> ExpectBadRequest)

            testCase "Should not be able to create a CAT with large whitespace title" (fun () ->            
                { CreateCat.Title="                 "; Owner=ownerGuid1 }
                |> ExpectBadRequest)
        ]

[<Tests>]
let catEditTests =
    let Expect: Result<Event,CQRSFailure> -> Event list * Command -> unit = Expect Events.applyEvent CommandHandling.handleCommand CommandHandling.nullCat 
    let ExpectSuccess event = Expect (Success event)
    let ExpectBadRequest = Expect (Failure (BadRequest "any string will do"))    

    let catHasBeenCreated = [CatCreated { Id = catGuid1; Title=PageTitle "My lovely crowdfunding ask template"; Owner=ownerGuid1; CreationTime = epoch }]

    testList "Cat editing tests"
        [
            testCase "Should be able to retitle a CAT" (fun () ->
                Given catHasBeenCreated
                |> When ( SetTitle { Id = catGuid1; Title="My lovely new cat name"; } )
                |> ExpectSuccess (TitleChanged { Id = catGuid1; Title=PageTitle "My lovely new cat name"; } ))

            testCase "Should not be able to change title to something too short" (fun () ->            
                Given catHasBeenCreated
                |> When ( SetTitle { Id = catGuid1; Title="a"; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change title to whitespace" (fun () ->            
                Given catHasBeenCreated
                |> When ( SetTitle { Id = catGuid1; Title="                   "; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change title of non-existent CAT" (fun () ->
                Given []
                |> When ( SetTitle { Id = catGuid1; Title="My lovely new cat name"; } )
                |> Expect (Failure NotFound))
        ]