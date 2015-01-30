module Cats.Domain.CommandsAndEvents

open Cats.Shared.ErrorHandling
open Cats.Domain.ICatRepository
open Cats.Domain.CatAggregate

open NodaTime
open System
open System.Collections.Generic

type State = int

type Command =
    | CreateCat of CreateCat

and CreateCat = {
    Something: int
   }

type Event =
    | CatCreated of CatCreated

and CatCreated = {
    Id: Guid
    CreationTime: Instant
}

type CommandFailure =
    | BadRequest of string
    | NotFound

let catCreated (repository:ICatRepository) (event: CatCreated) =
    repository.Put(
        {
            Cat.Id = event.Id
            CreationTime = event.CreationTime
        })

let handleEvent (repository : ICatRepository) = function
    | CatCreated event -> catCreated repository event

let createCat (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (command:CreateCat) =
    Success (CatCreated {
            Id = createGuid()
            CreationTime = createTimestamp()
        })

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command repository =
    match command with
        | CreateCat command -> createCat createGuid createTimestamp command

let handleCommandWithAutoGeneration command repository =
    handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command repository