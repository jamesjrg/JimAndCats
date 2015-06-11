module Jim.DuplicateEmailChecker.Program

open EventStore.YetAnotherClient
open FSharp.Data
open Jim.DuplicateEmailChecker.AppSettings
open System

[<Literal>]
let insertUser = "INSERT INTO Jim.UserEmail VALUES (@Id, @Email)"

[<Literal>]
let checkForDuplicateEmail =
    "if (EXISTS (SELECT * FROM Jim.UserEmail WHERE Email = @Email)) SELECT 1 ELSE SELECT 0"

type InsertUserCommand = SqlCommandProvider<insertUser, "name=Jim">
type CheckForDuplicateEmailQuery = SqlCommandProvider<checkForDuplicateEmail, "name=Jim">

type EmailAddress = EmailAddress of string

[<AutoOpen>]
module Events =
    type Event =
        | UserCreated of UserCreated
        | EmailChanged of EmailChanged
    
    
    (* Only deserialize the relevant parts of the domain model *)
    and UserCreated = {
        Id: Guid
        Email: EmailAddress
    }

    and EmailChanged = {
        Id: Guid
        Email: EmailAddress
    }

module private EventHandlers =
    let private userCreated (event: UserCreated) =
        async {
            ()
        }

    let private emailChanged (event : EmailChanged) =
        async {
            ()
        }

    let handleEvent = function
        | UserCreated event -> userCreated event
        | EmailChanged event -> emailChanged event

let subscribe() =
    let store = new EventStore<Event>(appSettings.PublicEventStoreIp, appSettings.PublicEventStorePort) :> IEventStore<Event>
    store.SubscribeToStreamFrom appSettings.PublicIdentityStream 0 (fun e -> EventHandlers.handleEvent e |> Async.RunSynchronously)

[<EntryPoint>]
let main argv = 
    subscribe()
    0
