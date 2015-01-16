module Jim.Tests.Domain

open Jim.Domain
open Jim.Tests.Specifications
open NodaTime
open System

open Fuchu

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let guid1 = createGuid1()

let createEpoch () = new Instant(0L)
let epoch = createEpoch()

let Expect = Specifications.expectWithCreationFuncs createGuid1 createEpoch

[<Tests>]
let tests =
    testList "Domain tests"
    [
        testCase "Should be able to create a user" (fun () ->            
            Given []
            |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
            |> Expect [ UserCreated { Id = guid1; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd"; CreationTime = epoch } ])

        testCase "Should be able to rename a user" (fun () ->
            Given [UserCreated { Id = guid1; Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd"; CreationTime = epoch }]
            |> When ( SetName { Id = guid1; Name="Bob Mariachi"; } )
            |> Expect [ NameChanged { Id = guid1; Name="Bob Mariachi"; } ])
    ]