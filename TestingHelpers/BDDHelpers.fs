module TestingHelpers.BDDHelpers

open GenericErrorHandling
open Swensen.Unquote.Assertions

let Given<'TEvent>(events: 'TEvent list) = events

let When<'TCommand, 'TEvent> (command: 'TCommand) (events:'TEvent list) = events, command

let Expect'<'TEvent, 'TCommand, 'TAggregate when 'TEvent:equality>
    (applyEvent: 'TAggregate -> 'TEvent -> 'TAggregate)
    (handleCommand: 'TCommand -> 'TAggregate option -> Result<'TEvent, CQRSFailure>)
    (initialState:'TAggregate)
    (expected: Result<'TEvent, CQRSFailure>)    
    (events, command) =

    let aggregate = List.fold applyEvent initialState events
    let actual = handleCommand command (Some aggregate)

    match expected, actual with
    | Failure (BadRequest e), Failure (BadRequest a) -> a =? a //not concerned about the precise error message
    | Failure NotFound, Failure NotFound -> Failure NotFound =? Failure NotFound
    | _ -> expected =? actual

