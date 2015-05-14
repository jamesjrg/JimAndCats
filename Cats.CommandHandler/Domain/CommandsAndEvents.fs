namespace Cats.CommandHandler.Domain

open GenericErrorHandling
open EventStore.YetAnotherClient
open Cats.CommandHandler.Domain

open NodaTime
open System

let minTitleLength = 3

[<AutoOpen>]
module Commands =
    type Command =
        | CreateCat of CreateCat
        | SetTitle of SetTitle

    and CreateCat = {
        Title: string
        Owner: Guid
       }

    and SetTitle = {
        Id: Guid
        Title: string
       }

[<AutoOpen>]
module Events =
    type Event =
        | CatCreated of CatCreated
        | TitleChanged of TitleChanged

    and CatCreated = {
        Id: Guid
        Title: PageTitle
        Owner: Guid
        CreationTime: Instant
    }

    and TitleChanged = {
        Id: Guid
        Title: PageTitle
    }

[<AutoOpen>]
module private EventHandlers =
    let catCreated (repository:ISimpleRepository<Cat>) (event: CatCreated) =
        async {
            repository.Put event.Id
                {
                    Cat.Id = event.Id
                    Title = event.Title
                    Owner = event.Owner
                    CreationTime = event.CreationTime
                }
        }

    let titleChanged (repository:ISimpleRepository<Cat>) (event: TitleChanged) =
        async {
            match repository.Get event.Id with
                | Some cat -> repository.Put event.Id {cat with Title = event.Title}
                | None -> ()
        }

[<AutoOpen>]
module PublicEventHandler = 
    let handleEvent (repository : ISimpleRepository<Cat>) = function
        | CatCreated event -> catCreated repository event
        | TitleChanged event -> titleChanged repository event

[<AutoOpen>]
module private CommandHandlers =
    let createTitle (s:string) =
        let trimmedTitle = s.Trim()
     
        if trimmedTitle.Length < minTitleLength then
            Failure (BadRequest (sprintf "Title must be at least %d characters" minTitleLength))
        else
            Success (PageTitle trimmedTitle)

    let createCat (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) (command:CreateCat) =
        async {
            return
                match createTitle command.Title with
                | Success title -> Success (CatCreated { Id = createGuid(); Title = title; Owner = command.Owner; CreationTime = createTimestamp()})
                | Failure f -> Failure f
        }

    let runCommandIfCatExists (repository : ISimpleRepository<Cat>) id command f =
        async {
        return
            match repository.Get id with
            | None -> Failure NotFound
            | _ -> f command
        }        

    let setTitle (command:SetTitle) =
        match createTitle command.Title with
        | Success title -> Success (TitleChanged { Id = command.Id; Title = title })
        | Failure f -> Failure f

[<AutoOpen>]
module PublicCommandHandlers = 
    let handleCommand (createGuid: unit -> Guid) (createTimestamp: unit -> Instant) command repository =
        match command with
            | CreateCat command -> createCat createGuid createTimestamp command
            | SetTitle command -> runCommandIfCatExists repository command.Id command setTitle

    let handleCommandWithAutoGeneration command repository =
        handleCommand Guid.NewGuid (fun () -> SystemClock.Instance.Now) command repository
    