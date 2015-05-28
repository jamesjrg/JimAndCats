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

let getCommandAgentAndAggregateBuilder() =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    let streamPrefix = "cat"
    let getAggregate = Repository.getAggregate store applyCommand streamPrefix
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent applyCommandWithAutoGeneration
    
    postCommand, getAggregate

let private runCommand postCommand (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand command

        match result with
        | Success (CatCreated event) ->
            return! jsonResponse Successful.CREATED ( { CatCreatedResponse.Id = event.Id; Message = "CAT created" }) httpContext
        | Success (TitleChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "CAT renamed" }) httpContext
        | Failure (BadRequest f) -> return! RequestErrors.BAD_REQUEST f httpContext
        | Failure NotFound -> return! genericNotFound httpContext
    }

let createCat postCommand (request:CreateCatRequest) =   
    runCommand postCommand (CreateCat { CreateCat.Title=request.title; Owner=request.Owner })

let setTitle postCommand (id:Guid) (request:SetTitleRequest) =   
    runCommand postCommand (SetTitle {Id=id; Title=request.title})

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

    let getCat getAggregate id =
        match getAggregate id with
        | Some cat -> jsonOK (mapCatToCatResponse cat)
        | None -> genericNotFound