module Cats.CommandEndpoints

open System

open MicroCQRS.Common
open MicroCQRS.Common.CommandFailure
open MicroCQRS.Common.Result
open MicroCQRS.Common.CommandAgent

open Cats.AppSettings
open Cats.InMemoryCatRepository
open Cats.CommandContracts
open Cats.Domain.CommandsAndEvents

open Suave
open Suave.Http
open Suave.Types
open Suave.Extensions.Json

(* If the system used a SQL database to maintain state for the user repository then the repository instance would not need to be shared between the command and query services, and the query service would not rely on the event store at all *)
let getCommandPosterAndRepository() =
    let streamId = appSettings.PrivateCatStream
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new MicroCQRS.Common.InMemoryStore<Event>() :> IEventStore<Event>
    let repository = new InMemoryCatRepository()
    let initialVersion = repository.Load(store, streamId) |> Async.RunSynchronously
    let postCommand = getCommandPoster store repository handleCommandWithAutoGeneration handleEvent streamId initialVersion   
    
    postCommand, repository

let runCommand postCommand (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand command

        match result with
        | Success (CatCreated event) ->
            return! jsonOK ( { CatCreatedResponse.Id = event.Id; Message = "CAT created" }) httpContext
        | Success (TitleChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "CAT renamed" }) httpContext
        | Failure (BadRequest f) -> return! RequestErrors.BAD_REQUEST f httpContext
        | Failure NotFound -> return! genericNotFound httpContext
    }

let createCat postCommand (request:CreateCatRequest) =   
    runCommand postCommand (CreateCat { CreateCat.Title=request.title })

let setTitle postCommand (id:Guid) (request:SetTitleRequest) =   
    runCommand postCommand (SetTitle {Id=id; Title=request.title})