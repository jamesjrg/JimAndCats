module Cats.Tests.Domain.CommandTests

open Cats.Shared.ErrorHandling
open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.Tests.Domain.Specifications
open NodaTime
open System

open Fuchu

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let guid1 = createGuid1()

let createEpoch () = new Instant(0L)
let epoch = createEpoch()

let Expect: Result<Event,string> -> Event list * Command -> unit = Specifications.expectWithCreationFuncs createGuid1 createEpoch
//let ExpectFailure = Specifications.expectWithCreationFuncs createGuid1 createEpoch (Failure "any string will do")
let ExpectSuccess event = Specifications.expectWithCreationFuncs createGuid1 createEpoch (Success event)

[<Tests>]
let tests =
    testList "Domain tests"
        [
            testCase "Should be able to create a CAT" (fun () ->            
                Given []
                |> When ( CreateCat { Something=5 } )
                |> ExpectSuccess (CatCreated { Id = guid1; CreationTime = epoch } ))
        ]