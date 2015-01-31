module Cats.Domain.CommandsAndEvents

open Cats.Result
open Cats.Domain.ICatRepository
open Cats.Domain.CatAggregate

open NodaTime
open System
open System.Collections.Generic

let minTitleLength = 3

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

let createTitle (s:string) =
    let trimmedTitle = s.Trim()
     
    if trimmedTitle.Length < minTitleLength then
        Failure (sprintf "Title must be at least %d characters" minTitleLength)
    else
        Success (PageTitle trimmedTitle)

let createCat (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (command:CreateCat) =
    match createTitle command.Title with
    | Success title -> Success (CatCreated { Id = createGuid(); Title = title; CreationTime = createTimestamp()})
    | Failure f -> Failure (BadRequest f)

let runCommandIfCatExists (repository : ICatRepository) id command f =
    match repository.Get(id) with
    | None -> Failure NotFound
    | _ -> f command

let setTitle (command:SetTitle) =
    match createTitle command.Title with
    | Success title -> Success (TitleChanged { Id = command.Id; Title = PageTitle command.Title })
    | Failure f -> Failure (BadRequest f)

let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command repository =
    match command with
        | CreateCat command -> createCat createGuid createTimestamp command
        | SetTitle command -> runCommandIfCatExists repository command.Id command setTitle

let handleCommandWithAutoGeneration command repository =
    handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command repository