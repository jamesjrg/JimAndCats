module Cats.Tests.Integration.CreateWebServer

open MicroCQRS.Common

open Cats
open Cats.Domain.CommandsAndEvents
open Cats.Domain.CatAggregate
open Cats.InMemoryCatRepository
open Cats.WebServer

let streamId = "testStream"

let getWebServer events =
    let store = MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    if not (List.isEmpty events) then
        store.AppendToStream streamId -1 events |> Async.RunSynchronously
    let repository = new InMemoryCatRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    let postCommand, repo = (CommandAgent.getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion), repository
    webApp postCommand repo