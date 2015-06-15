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

let getAppServices() =
    let store = 
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> 
            new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    
    let streamPrefix = "cat"
    let getAggregate = Repository.getAggregate store applyEvent CommandHandling.invalidCat streamPrefix
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = 
        EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent 
            CommandHandling.handleCommand
    postCommand, getAggregate, Repository.saveEventToNewStream store streamPrefix

let mapResultToResponse = function
    | Success (CatCreated event) ->
        jsonResponse Successful.CREATED ( { CatCreatedResponse.Id = event.Id; Message = "CAT created" })
    | Success (TitleChanged event) ->
        jsonOK ( { GenericResponse.Message = "CAT renamed" })
    | Failure (BadRequest f) -> RequestErrors.BAD_REQUEST f
    | Failure NotFound -> genericNotFound

let createCommandToWebPartMapper (commandAgent : Guid -> Command -> Async<Result<Event, CQRSFailure>>) (aggregateId:Guid) command : Types.WebPart =
    fun httpContext ->
        async {
            let! result = commandAgent aggregateId command
            return! mapResultToResponse result httpContext
        }   

let setTitle (commandToWebPart : Guid -> Command -> Types.WebPart) (aggregateId:Guid) (request:SetTitleRequest) : Types.WebPart =
    commandToWebPart aggregateId (SetTitle {Id=aggregateId; Title=request.title})

let createCat (saveEventToNewStream : Guid -> Event -> Async<unit>) (request:CreateCatRequest) =
    fun httpContext -> async {
        let result = CommandHandling.createCatWithAutoGeneration ({ CreateCat.Title=request.title; Owner=request.Owner })

        match result with
        | Success event ->
            let wrappedEvent = CatCreated event
            do! saveEventToNewStream event.Id wrappedEvent
            return! mapResultToResponse (Success wrappedEvent) httpContext
        | Failure f ->
            return! mapResultToResponse (Failure f) httpContext
    }            

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

    let getCat (getAggregate: Guid -> Async<Cat option * int>) id : Types.WebPart =
        fun httpContext -> async {
            let! result = getAggregate id
            return!
                match fst result with
                | Some cat -> jsonOK (mapCatToCatResponse cat) httpContext
                | None -> genericNotFound httpContext
        }