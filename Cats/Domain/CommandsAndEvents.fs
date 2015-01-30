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
    | SetTitle of SetTitle

and CreateCat = {
    Title: string
   }

and SetTitle = {
    Id: Guid
    Title: string
   }

type Event =
    | CatCreated of CatCreated
    | TitleChanged of TitleChanged

and CatCreated = {
    Id: Guid
    Title: PageTitle
    CreationTime: Instant
}

and TitleChanged = {
    Id: Guid
    Title: PageTitle
}

type CommandFailure =
    | BadRequest of string
    | NotFound

let catCreated (repository:ICatRepository) (event: CatCreated) =
    repository.Put(
        {
            Cat.Id = event.Id
            Title = event.Title
            CreationTime = event.CreationTime
        })

let titleChanged (repository:ICatRepository) (event: TitleChanged) =
    match repository.Get(event.Id) with
        | Some cat -> repository.Put({cat with Title = event.Title})
        | None -> ()

let handleEvent (repository : ICatRepository) = function
    | CatCreated event -> catCreated repository event
    | TitleChanged event -> titleChanged repository event

let createCat (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (command:CreateCat) =
    Success (CatCreated {
            Id = createGuid()
            Title = PageTitle command.Title
            CreationTime = createTimestamp()
        })

let setTitle (command:SetTitle) =
    Success (TitleChanged {
            Id = command.Id
            Title = PageTitle command.Title
        })

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command repository =
    match command with
        | CreateCat command -> createCat createGuid createTimestamp command
        | SetTitle command -> setTitle command

let handleCommandWithAutoGeneration command repository =
    handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command repository