namespace Cats.CommandHandler.Domain

open GenericErrorHandling
open Cats.CommandHandler.Domain
open NodaTime
open System

[<AutoOpen>]
module Commands = 
    type Command = 
        | SetTitle of SetTitle
    
    and SetTitle = 
        { Id : Guid
          Title : string }
    
    type CreateCat = 
        { Title : string
          Owner : Guid }

[<AutoOpen>]
module Events = 
    type Event = 
        | CatCreated of CatCreated
        | TitleChanged of TitleChanged
    
    and CatCreated = 
        { Id : Guid
          Title : PageTitle
          Owner : Guid
          CreationTime : Instant }
    
    and TitleChanged = 
        { Id : Guid
          Title : PageTitle }

    let applyEvent (cat : Cat) = function 
        | CatCreated createCat ->
            {
                Cat.Id = createCat.Id
                Title = createCat.Title
                Owner = createCat.Owner
                CreationTime = createCat.CreationTime
            }
        | TitleChanged eventDetails -> { cat with Title = eventDetails.Title }

[<AutoOpen>]
module private InternalCommandHandling = 
    let minTitleLength = 3
    
    let createTitle (s : string) = 
        let trimmedTitle = s.Trim()
        if trimmedTitle.Length < minTitleLength then 
            Failure(BadRequest(sprintf "Title must be at least %d characters" minTitleLength))
        else Success(PageTitle trimmedTitle)
    
    let setTitle (command : SetTitle) = 
        match createTitle command.Title with
        | Success title -> 
            Success(TitleChanged { Id = command.Id
                                   Title = title })
        | Failure f -> Failure f

module CommandHandling = 
    let handleCommand command (maybeCat : Cat option) = 
        match maybeCat with
        | Some cat -> 
            match command with
            | SetTitle command -> setTitle command
        | None -> Failure NotFound
    
    let createCat (createGuid : unit -> Guid) (createTimestamp : unit -> Instant) (command : CreateCat) = 
        match createTitle command.Title with
        | Success title -> 
            Success(
                {
                    CatCreated.Id = createGuid()
                    Title = title
                    Owner = command.Owner
                    CreationTime = createTimestamp()
                })
        | Failure f -> Failure f
    
    let createCatWithAutoGeneration command = createCat Guid.NewGuid (fun () -> SystemClock.Instance.Now) command
    
    let invalidCat = 
        { Cat.CreationTime = SystemClock.Instance.Now
          Id = Guid.Empty
          Title = PageTitle "Invalid"
          Owner = Guid.Empty }
