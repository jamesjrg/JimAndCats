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
                |> ExpectSuccess [ UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch } ])

            testCase "Should not be able to create a user with too short a username" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectFailure)

            testCase "Should not be able to create a user with large whitespace username" (fun () ->            
                Given []
                |> When ( CreateUser { Name="                 "; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectFailure)

            testCase "Should not be able to create a user with invalid email address" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holnessitv.com"; Password="p4ssw0rd" } )
                |> ExpectFailure)

            testCase "Should be able to rename a user" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetName { Id = guid1; Name="Bob Mariachi"; } )
                |> ExpectSuccess [ NameChanged { Id = guid1; Name=Username "Bob Mariachi"; } ])

            testCase "Should not be able to change name to too short a username" (fun () ->            
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetName { Id = guid1; Name="Bob"; } )
                |> ExpectFailure)

            testCase "Should not be able to change name to large amount of whitespace" (fun () ->            
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetName { Id = guid1; Name="                   "; } )
                |> ExpectFailure)

            testCase "Usernames should be trimmed" (fun () ->            
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetName { Id = guid1; Name="        hello           "; } )
                |> ExpectSuccess [ NameChanged { Id = guid1; Name=Username "hello"; } ])

            testCase "Should be able to change email" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="bob@abc.com"; } )
                |> ExpectSuccess [ EmailChanged { Id = guid1; Email=EmailAddress "bob@abc.com"; } ])

            testCase "Email should be canonicalized without whitespace or capital letters" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="   BoB@abc.com"     ; } )
                |> ExpectSuccess [ EmailChanged { Id = guid1; Email=EmailAddress "bob@abc.com"; } ])

            testCase "Should not be able to change email to invalid email address" (fun () ->            
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="bobabc.com"; } )
                |> ExpectFailure)

            testCase "Should be able to change password" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetPassword { Id = guid1; Password="n3wp4ss"; } )
                |> ExpectSuccess [ PasswordChanged { Id = guid1; PasswordHash=PasswordHash "n3wp4ss"; } ])

            testCase "Passwords should be trimmed before hashing" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetPassword { Id = guid1; Password="    n3wp4ss   "; } )
                |> ExpectSuccess [ PasswordChanged { Id = guid1; PasswordHash=PasswordHash "n3wp4ss"; } ])

            testCase "Should be able to change password to lots of whitespace" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetPassword { Id = guid1; Password="                 "; } )
                |> ExpectFailure)
        ]