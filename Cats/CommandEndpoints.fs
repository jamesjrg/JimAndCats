module Cats.CommandEndpoints

open System
open EventPersistence
open Cats.Shared.ErrorHandling
open Cats.AppSettings
open Cats.InMemoryCatRepository
open Cats.CommandAgent
open Cats.Domain.CommandsAndEvents
open Suave
open Suave.Types
open Suave.Extensions.Json

(* If the system used a SQL database to maintain state for the user repository then the repository instance would not need to be shared between the command and query services, and the query service would not rely on the event store at all *)
let getCommandPosterAndRepository() =
    let streamId = appSettings.PrivateCatStream
    let store =
        match appSettings.UseEventStore with
        | true -> new EventPersistence.EventStore<Event>(streamId) :> IEventStore<Event>
        | false -> new EventPersistence.InMemoryStore<Event>() :> IEventStore<Event>
    let repository = new InMemoryCatRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    let postCommand = getCommandPoster store repository streamId initialVersion
    
    postCommand, repository

let runCommand postCommand (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand command

        return! jsonOK "TODO" httpContext
    }

let createCat postCommand () =   
    runCommand postCommand (SomethingCommand {Something=5})