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

let getAppServices() =
    let store =
        match appSettings.WriteToInMemoryStoreOnly with
        | false -> new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    let streamPrefix = "user"
    let getAggregate = Repository.getAggregate store applyCommandWithAutoGeneration streamPrefix invalidUser
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent applyCommandWithAutoGeneration
    
    postCommand, getAggregate, saveEvent

let mapResultToResponse = function
    | Success (UserCreated event) ->
        jsonResponse Successful.CREATED ( { UserCreatedResponse.Id = event.Id; Message = "User created: " + extractUsername event.Name })
    | Success (NameChanged event) ->
        jsonOK ( { GenericResponse.Message = "Name changed to: " + extractUsername event.Name })
    | Success (EmailChanged event) ->
        jsonOK ( { GenericResponse.Message = "Email changed to: " + extractEmail event.Email })
    | Success (PasswordChanged event) ->
        jsonOK ( { GenericResponse.Message = "Password changed" })
    | Failure (BadRequest f) -> RequestErrors.BAD_REQUEST f
    | Failure NotFound -> genericNotFound


let private runCommand postCommand userId (command:Command) : Types.WebPart =
    fun httpContext ->
    async {
        let! result = postCommand userId command
        return! mapResultToResponse result httpContext
    }

let createUser saveEvent (requestDetails:CreateUserRequest) =   
    let result = PublicCommandHandlers.createUserWithAutoGeneration (CreateUser {Name=requestDetails.name; Email=requestDetails.email; Password=requestDetails.password})

    match result with
    | Success event -> saveEvent event
    | Failure f -> ()

    mapResultToResponse result

let setName postCommand (userId:Guid) (requestDetails:SetNameRequest) =    
    runCommand postCommand userId (SetName{ Id=userId; Name = requestDetails.name})

let setEmail postCommand (userId:Guid) (requestDetails:SetEmailRequest) =
    runCommand postCommand userId ( SetEmail {Id = userId; Email = requestDetails.email} )

let setPassword postCommand (userId:Guid) (requestDetails:SetPasswordRequest) =    
    runCommand postCommand userId ( SetPassword{ Id=userId; Password = requestDetails.password})

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

    let getUser getAggregate id : Suave.Types.WebPart =
        fun httpContext ->
            async {
                let! result = getAggregate id
                return!
                    match fst result with
                    | Some user -> jsonOK (mapUserToUserResponse user) httpContext
                    | None -> genericNotFound httpContext
            }

