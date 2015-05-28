module Jim.CommandHandler.AppService

open System

open Jim.CommandHandler.AppSettings
open Jim.CommandHandler.CommandContracts
open Jim.CommandHandler.Domain

open EventStore.YetAnotherClient
open GenericErrorHandling

open Suave
open Suave.Http
open Suave.Extensions.Json

let getCommandAgentAndAggregateBuilder() =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    let streamPrefix = "user"
    let getAggregate = Repository.getAggregate store applyCommand streamPrefix
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent applyCommandWithAutoGeneration
    
    postCommand, getAggregate        

let private runCommand postCommand (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand command

        match result with
        | Success (UserCreated event) ->
            return! jsonResponse Successful.CREATED ( { UserCreatedResponse.Id = event.Id; Message = "User created: " + extractUsername event.Name }) httpContext
        | Success (NameChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Name changed to: " + extractUsername event.Name }) httpContext
        | Success (EmailChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Email changed to: " + extractEmail event.Email }) httpContext
        | Success (PasswordChanged event) ->
            return! jsonOK ( { GenericResponse.Message = "Password changed" }) httpContext
        | Failure (BadRequest f) -> return! RequestErrors.BAD_REQUEST f httpContext
        | Failure NotFound -> return! genericNotFound httpContext
    }

let createUser postCommand (requestDetails:CreateUserRequest) =   
    runCommand postCommand (CreateUser {Name=requestDetails.name; Email=requestDetails.email; Password=requestDetails.password})

let setName postCommand (id:Guid) (requestDetails:SetNameRequest) =    
    runCommand postCommand (SetName{ Id=id; Name = requestDetails.name})

let setEmail postCommand (id:Guid) (requestDetails:SetEmailRequest) =
    runCommand postCommand ( SetEmail {Id = id; Email = requestDetails.email} )

let setPassword postCommand (id:Guid) (requestDetails:SetPasswordRequest) =    
    runCommand postCommand ( SetPassword{ Id=id; Password = requestDetails.password})

(* These methods are just utility methods for debugging etc, services should listen to Event Store events and build their own read models *)
module DiagnosticQueries =
    type GetUserResponse = {
        Id: Guid
        Name: string
        Email: string
        CreationTime: string
    }

    type GetUsersResponse = {
        Users: GetUserResponse seq
    }

    let mapUserToUserResponse (user:User) =
        {
            GetUserResponse.Id = user.Id
            Name = extractUsername user.Name
            Email = extractEmail user.Email
            CreationTime = user.CreationTime.ToString()
        } 

    let getUser (getAggregate : Guid-> Async<'TAggregate option>) id : Suave.Types.WebPart =
        fun httpContext ->
            async {
                let! result = getAggregate id
                return!
                    match result with
                    | Some user -> jsonOK (mapUserToUserResponse user) httpContext
                    | None -> genericNotFound httpContext
            }

