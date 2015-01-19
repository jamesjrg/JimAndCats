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
let identityHash s = s

let Expect = Specifications.expectWithCreationFuncs createGuid1 createEpoch identityHash
let ExpectFailure = Specifications.expectWithCreationFuncs createGuid1 createEpoch identityHash (Failure "any string will do")
let ExpectSuccess events = Specifications.expectWithCreationFuncs createGuid1 createEpoch identityHash (Success events)

[<Tests>]
let tests =
    testList "Domain tests"
        [
            testCase "Should be able to create a user" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectSuccess [ UserCreated { Id = guid1; Name="Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch } ])

            testCase "Should not be able to create a user with invalid email address" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holnessitv.com"; Password="p4ssw0rd" } )
                |> ExpectFailure)

            testCase "Should be able to rename a user" (fun () ->
                Given [UserCreated { Id = guid1; Name="Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetName { Id = guid1; Name="Bob Mariachi"; } )
                |> ExpectSuccess [ NameChanged { Id = guid1; Name="Bob Mariachi"; } ])

            testCase "Should be able to change email" (fun () ->
                Given [UserCreated { Id = guid1; Name="Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="bob@abc.com"; } )
                |> ExpectSuccess [ EmailChanged { Id = guid1; Email=EmailAddress "bob@abc.com"; } ])

            testCase "Should not be able to change email to invalid email address" (fun () ->            
                Given [UserCreated { Id = guid1; Name="Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="bobabc.com"; } )
                |> ExpectFailure)
        ]