module Jim.Tests.Integration.CreateWebServer

open Jim
open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.WebServer
open MicroCQRS.Common

let streamId = "testStream"

let getWebServer events =
    let store = MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new SimpleInMemoryRepository<User>()
    let initialVersion = repository.Load<Event>(store, streamId, handleEvent) |> Async.RunSynchronously
    let postCommand, repo = (CommandAgent.getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion), repository
    webApp postCommand repo