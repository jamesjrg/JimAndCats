module Cats.CommandHandler.AppService

open System

open EventStore.YetAnotherClient
open GenericErrorHandling

open Cats.CommandHandler.AppSettings
open Cats.CommandHandler.CommandContracts
open Cats.CommandHandler.Domain

open Suave
open Suave.Http
open Suave.Extensions.Json
open Suave.Types

let getAppServices() =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    let streamPrefix = "cat"
    let getAggregate = Repository.getAggregate store applyCommand invalidCat streamPrefix
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent applyCommand
    
    postCommand, getAggregate, saveEvent

let mapResultToResponse = function
    | Success (CatCreated event) ->
        jsonResponse Successful.CREATED ( { CatCreatedResponse.Id = event.Id; Message = "CAT created" })
    | Success (TitleChanged event) ->
        jsonOK ( { GenericResponse.Message = "CAT renamed" })
    | Failure (BadRequest f) -> RequestErrors.BAD_REQUEST f
    | Failure NotFound -> genericNotFound

let createCat saveEvent (request:CreateCatRequest) =
    let result = PublicCommandHandlers.createCatWithAutoGeneration (CreateCat { CreateCat.Title=request.title; Owner=request.Owner })

    match result with
    | Success event -> saveEvent event
    | Failure f -> ()

    mapResultToResponse result

let setTitle postCommand (aggregateId:Guid) (request:SetTitleRequest) =
    postCommand aggregateId (SetTitle {Id=aggregateId; Title=request.title})

(* These methods are just utility methods for debugging etc, services should listen to Event Store events and build their own read models *)
module DiagnosticQueries =
    type GetCatResponse = {
        Id: Guid
        CreationTime: string
    }

    let mapCatToCatResponse (cat:Cat) =
        {
            GetCatResponse.Id = cat.Id
            CreationTime = cat.CreationTime.ToString()
        }

    let getCat getAggregate id : WebPart =
        fun httpContext -> async {
            let! result = getAggregate id |> fst
            return!
                match result with
                | Some cat -> jsonOK (mapCatToCatResponse cat) httpContext
                | None -> genericNotFound httpContext
        }