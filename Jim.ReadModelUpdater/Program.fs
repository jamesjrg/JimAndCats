module Jim.ReadModelUpdater.Program

open Jim.ReadModelUpdater.AppSettings
open Jim.Domain
open Jim.UserRepository
open EventStore.YetAnotherClient
open NodaTime
open System

[<AutoOpen>]
module Events =
    type Event =
        | UserCreated of UserCreated
        | NameChanged of NameChanged
        | EmailChanged of EmailChanged
        | PasswordChanged of PasswordChanged
    
    and UserCreated = {
        Id: Guid
        Name: Username
        Email: EmailAddress
        PasswordHash: PasswordHash
        CreationTime: Instant
    }

    and NameChanged = {
        Id: Guid
        Name: Username
    }

    and EmailChanged = {
        Id: Guid
        Email: EmailAddress
    }

    and PasswordChanged = {
        Id: Guid
        PasswordHash: PasswordHash
    }

module private EventHandlers =
    let private userCreated (repository:IUserRepository) (event: UserCreated) =
        repository.Put
            {
                User.Id = event.Id
                Name = event.Name
                Email = event.Email
                PasswordHash = event.PasswordHash
                CreationTime = event.CreationTime
            }

    let private nameChanged (repository:IUserRepository) (event : NameChanged) =
        async {
            let! maybeUser = repository.Get(event.Id)
            match maybeUser with
            | Some user -> repository.Put {user with Name = event.Name} |> ignore
            | None -> ()
        }

    let private emailChanged (repository:IUserRepository) (event : EmailChanged) =
        async {
            let! maybeUser = repository.Get(event.Id)
            match maybeUser with
            | Some user -> repository.Put {user with Email = event.Email} |> ignore
            | None -> ()
        }

    let private passwordChanged (repository:IUserRepository) (event : PasswordChanged) =
        async {
            let! maybeUser = repository.Get(event.Id)
            match maybeUser with
            | Some user -> repository.Put {user with PasswordHash = event.PasswordHash} |> ignore
            | None -> ()
        }

    let handleEvent (repository : IUserRepository) = function
        | UserCreated event -> userCreated repository event
        | NameChanged event -> nameChanged repository event
        | EmailChanged event -> emailChanged repository event
        | PasswordChanged event -> passwordChanged repository event

module private JimModel =
    let subscribe() =
        let store = new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        let repository = new SqlServer.UserRepository()
        store.SubscribeToStreamFrom appSettings.PrivateIdentityStream 0 (fun e -> EventHandlers.handleEvent repository e |> Async.RunSynchronously)

[<EntryPoint>]
let main argv = 
    JimModel.subscribe()
    0
