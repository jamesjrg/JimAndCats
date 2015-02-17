module TestingHelpers.BDDHelpers

open GenericErrorHandling

open Swensen.Unquote.Assertions

let inline replay (handleEvent: 'TRepository -> 'TEvent -> unit) events repository =
    List.iter (handleEvent repository) events

let Given<'TEvent>(events: 'TEvent list) = events

let When<'TCommand, 'TEvent> (command: 'TCommand) (events:'TEvent list) = events, command

let Expect'<'TEvent, 'TCommand, 'TRepository when 'TEvent:equality>
    (getRepository: unit -> 'TRepository)
    (handleEvent: 'TRepository -> 'TEvent -> unit)
    (handleCommand: 'TCommand -> 'TRepository -> Result<'TEvent, CQRSFailure>)
    (expected: Result<'TEvent, CQRSFailure>)
    (events, command) =

    let repository = getRepository()  
    replay handleEvent events repository
    let actual = handleCommand command repository

    match expected, actual with
    | Failure (BadRequest e), Failure (BadRequest a) -> a =? a //not concerned about the precise error message
    | Failure NotFound, Failure NotFound -> Failure NotFound =? Failure NotFound
    | _ -> expected =? actual

