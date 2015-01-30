module Cats.Shared.ErrorHandling

type Result<'a, 'b> = 
    | Success of 'a
    | Failure of 'b

let bind func result = 
    match result with
    | Success s -> func s
    | Failure f -> Failure f

let (>>=) twoTrackInput switchFunction = 
    bind switchFunction twoTrackInput
