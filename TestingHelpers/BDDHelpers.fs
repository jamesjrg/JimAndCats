module TestingHelpers.BDDHelpers

open GenericErrorHandling

open Swensen.Unquote.Assertions

let inline replay (handleEvent: 'TRepository -> 'TEvent -> Async<unit>) events repository =
    List.iter (fun event -> handleEvent repository event |> Async.RunSynchronously) events

let Given<'TEvent>(events: 'TEvent list) = events

let When<'TCommand, 'TEvent> (command: 'TCommand) (events:'TEvent list) = events, command

let Expect'<'TEvent, 'TCommand, 'TRepository when 'TEvent:equality>
    (getRepository: unit -> 'TRepository)
    (handleEvent: 'TRepository -> 'TEvent -> Async<unit>)
    (applyCommand: 'TCommand -> 'TRepository -> Async<Result<'TEvent, CQRSFailure>>)
    (expected: Result<'TEvent, CQRSFailure>)
    (events, command) =

    let repository = getRepository()  
    replay handleEvent events repository
    let actual = applyCommand command repository |> Async.RunSynchronously

    match expected, actual with
    | Failure (BadRequest e), Failure (BadRequest a) -> a =? a //not concerned about the precise error message
    | Failure NotFound, Failure NotFound -> Failure NotFound =? Failure NotFound
    | _ -> expected =? actual

