module Jim.AppServiceInit

open Jim.AppSettings
open Jim.Domain.CommandsAndEvents
open Jim.InMemoryUserRepository
open Jim.CommandApplicationService
open Jim.QueryApplicationService
open EventPersistence

(* If the system used a SQL database to maintain external in user repository then the repository would not need to be shared between the
    command and query services, and the query service would not rely on the event store at all *)

let getAppServices() =
    let streamId = appSettings.UserStream
    let store =
        match appSettings.UseEventStore with
        | true -> new EventPersistence.EventStore<Event>(streamId) :> IEventStore<Event>
        | false -> new EventPersistence.InMemoryStore<Event>() :> IEventStore<Event>
    let repository = new InMemoryUserRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    let commandAppService = new CommandAppService(store, repository, streamId, initialVersion)
    let queryAppService = new QueryAppService(repository)
    
    commandAppService, queryAppService