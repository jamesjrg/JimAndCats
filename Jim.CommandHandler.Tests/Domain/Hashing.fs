module Jim.Tests.Domain.Hashing

open Jim.CommandHandler.Domain.AuthenticationService.PBKDF2

open Fuchu
open Swensen.Unquote.Assertions

[<Tests>]
let tests =
    testList "Hashing tests"
        [
            testCase "Should be able to match a hashed password" (fun () ->            
                let password = "sxjdfls312w3w"
                let hash = getHash password
                validatePassword hash password =? true)

            testCase "Should not be able to match a hashed password with a truncated string" (fun () ->            
                let password = "sxjdfls312w3w"
                let incorrectPassword = "sxjdfls312w3"
                let hash = getHash password
                validatePassword hash incorrectPassword =? false) 

            testCase "Should not be able to match a hashed password with a different string of same length" (fun () ->            
                let password = "sxjdfls312w3w"
                let incorrectPassword = "sxjdfls312w3x"
                let hash = getHash password
                validatePassword hash incorrectPassword =? false)
        ]