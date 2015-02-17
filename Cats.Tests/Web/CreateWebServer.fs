module Cats.Tests.Web.CreateWebServer

open Cats
open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.Tests.AppSettings
open Cats.WebServer
open EventStore.YetAnotherClient

let streamId = "testStream"

let getWebServer events =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new SimpleInMemoryRepository<Cat>()
    let initialVersion = repository.Load<Event>(store, streamId, handleEvent) |> Async.RunSynchronously
    let postCommand, repo = (CommandAgent.getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion), repository
    webApp postCommand repo