module Jim.Tests.Domain.CommandTests

open MicroCQRS.Common
open MicroCQRS.Common.CommandFailure
open MicroCQRS.Common.Result
open MicroCQRS.Common.Testing.BDDHelpers
open Jim
open Jim.Domain
open NodaTime
open System

open Fuchu

let createGuid1 () = new Guid("3C71C09A-2902-4682-B8AB-663432C8867B")
let guid1 = createGuid1()

let createEpoch () = new Instant(0L)
let epoch = createEpoch()
let identityHash s = s

let Expect = Expect' (fun () -> new UserRepository()) handleEvent (handleCommand createGuid1 createEpoch identityHash)
let ExpectBadRequest = Expect (Failure (BadRequest "any string will do"))
let ExpectSuccess event = Expect (Success event)

let GivenBobHolness = Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]

[<Tests>]
let tests =
    testList "Domain tests"
        [
            testCase "Should be able to create a user" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectSuccess (UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch } ))

            testCase "Should not be able to create a user with too short a username" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectBadRequest)

            testCase "Should not be able to create a user with large whitespace username" (fun () ->            
                Given []
                |> When ( CreateUser { Name="                 "; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectBadRequest)

            testCase "Should not be able to create a user with invalid email address" (fun () ->            
                Given []
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holnessitv.com"; Password="p4ssw0rd" } )
                |> ExpectBadRequest)

            testCase "Should not be able to create a user with same email as existing user" (fun () ->            
                GivenBobHolness
                |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
                |> ExpectBadRequest)

            testCase "Should be able to rename a user" (fun () ->
                GivenBobHolness
                |> When ( SetName { Id = guid1; Name="Bob Mariachi"; } )
                |> ExpectSuccess (NameChanged { Id = guid1; Name=Username "Bob Mariachi"; } ))

            testCase "Should not be able to change name to too short a username" (fun () ->            
                GivenBobHolness
                |> When ( SetName { Id = guid1; Name="Bob"; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change name to large amount of whitespace" (fun () ->            
                GivenBobHolness
                |> When ( SetName { Id = guid1; Name="                   "; } )
                |> ExpectBadRequest)

            testCase "Usernames should be trimmed" (fun () ->            
                GivenBobHolness
                |> When ( SetName { Id = guid1; Name="        hello           "; } )
                |> ExpectSuccess ( NameChanged { Id = guid1; Name=Username "hello"; } ))

            testCase "Should be able to change email" (fun () ->
                GivenBobHolness
                |> When ( SetEmail { Id = guid1; Email="bob@abc.com"; } )
                |> ExpectSuccess ( EmailChanged { Id = guid1; Email=EmailAddress "bob@abc.com"; } ))

            testCase "Email should be canonicalized without whitespace or capital letters" (fun () ->
                Given [UserCreated { Id = guid1; Name=Username "Bob Holness"; Email=EmailAddress "bob.holness@itv.com"; PasswordHash=PasswordHash "p4ssw0rd"; CreationTime = epoch }]
                |> When ( SetEmail { Id = guid1; Email="   BoB@abc.com"     ; } )
                |> ExpectSuccess ( EmailChanged { Id = guid1; Email=EmailAddress "bob@abc.com"; } ))

            testCase "Should not be able to change email to invalid email address" (fun () ->            
                GivenBobHolness
                |> When ( SetEmail { Id = guid1; Email="bobabc.com"; } )
                |> ExpectBadRequest)

            testCase "Should be able to change password" (fun () ->
                GivenBobHolness
                |> When ( SetPassword { Id = guid1; Password="n3wp4ss"; } )
                |> ExpectSuccess ( PasswordChanged { Id = guid1; PasswordHash=PasswordHash "n3wp4ss"; } ))

            testCase "Passwords should be trimmed before hashing" (fun () ->
                GivenBobHolness
                |> When ( SetPassword { Id = guid1; Password="    n3wp4ss   "; } )
                |> ExpectSuccess ( PasswordChanged { Id = guid1; PasswordHash=PasswordHash "n3wp4ss"; } ))

            testCase "Should be able to change password to lots of whitespace" (fun () ->
                GivenBobHolness
                |> When ( SetPassword { Id = guid1; Password="                 "; } )
                |> ExpectBadRequest)

            testCase "Should not be able to change name of non-existent user" (fun () ->
                Given []
                |> When ( SetName { Id = guid1; Name="flibbles123"; } )
                |> Expect (Failure NotFound))

            testCase "Should not be able to change email of non-existent user" (fun () ->
                Given []
                |> When ( SetEmail { Id = guid1; Email="a@b.com"; } )
                |> Expect (Failure NotFound))

            testCase "Should not be able to change password of non-existent user" (fun () ->
                Given []
                |> When ( SetPassword { Id = guid1; Password="flibbles123"; } )
                |> Expect (Failure NotFound))
        ]