module Jim.DuplicateCheckingUpdater.Program

open Jim.Domain
open FSharp.Data
open System

[<Literal>]
let insertUser = "INSERT INTO Jim.UserEmail VALUES (@Id, @Email)"

type InsertUserCommand = SqlCommandProvider<insertUser, "name=Jim">

[<AutoOpen>]
module Events =
    type Event =
        | UserCreated of UserCreated
        | EmailChanged of EmailChanged
    
    and UserCreated = {
        Id: Guid
        Name: Username
        Email: EmailAddress
        PasswordHash: PasswordHash
        CreationTime: Instant
    }

    and EmailChanged = {
        Id: Guid
        Email: EmailAddress
    }

module private EventHandlers =
    let private userCreated (event: UserCreated) =
        ()

    let private emailChanged (repository:IUserRepository) (event : EmailChanged) =
        ()

    let handleEvent = function
        | UserCreated event -> userCreated repository event
        | EmailChanged event -> emailChanged repository event

let subscribe() =
    let store = new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
    let repository = new SqlServer.UserRepository()
    store.SubscribeToStreamFrom appSettings.PrivateIdentityStream 0 (fun e -> EventHandlers.handleEvent repository e |> Async.RunSynchronously)

[<EntryPoint>]
let main argv = 
    subscribe()
    0
