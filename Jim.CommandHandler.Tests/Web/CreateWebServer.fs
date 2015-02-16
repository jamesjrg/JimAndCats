module Jim.Tests.Web.CreateWebServer

open Jim
open Jim.Domain
open Jim.Tests.AppSettings
open Jim.CommandHandler.WebServer
open Jim.UserRepository
open MicroCQRS.Common

let streamId = "testStream"

let getWebServer events =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new InMemoryUserRepository()
    let initialVersion = repository.Load(store, streamId, handleEvent) |> Async.RunSynchronously
    let postCommand, repo = (CommandAgent.getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion), repository
    webApp postCommand repo