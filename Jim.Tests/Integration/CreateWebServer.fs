module Jim.Tests.Integration.CreateWebServer

open MicroCQRS.Common

open Jim
open Jim.Domain.CommandsAndEvents
open Jim.Domain.UserAggregate
open Jim.InMemoryUserRepository
open Jim.WebServer

let streamId = "testStream"

let getWebServer events =
    let store = MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new InMemoryUserRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    let postCommand, repo = (CommandAgent.getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion), repository
    webApp postCommand repo