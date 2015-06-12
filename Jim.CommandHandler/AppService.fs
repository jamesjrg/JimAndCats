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
        | false -> 
            new EventStore<Event>(appSettings.PrivateEventStoreIp, appSettings.PrivateEventStorePort) :> IEventStore<Event>
        | true -> new InMemoryStore<Event>() :> IEventStore<Event>
    
    let streamPrefix = "user"
    let getAggregate = Repository.getAggregate store applyEvent CommandHandling.invalidUser streamPrefix
    let saveEvent = Repository.saveEvent store streamPrefix
    let postCommand = 
        EventStore.YetAnotherClient.CommandAgent.getCommandAgent getAggregate saveEvent 
            CommandHandling.handleCommandWithAutoGeneration
    postCommand, getAggregate, Repository.saveEventToNewStream store streamPrefix

let mapResultToResponse = 
    function 
    | Success(UserCreated event) -> 
        jsonResponse Successful.CREATED ({ UserCreatedResponse.Id = event.Id
                                           Message = "User created: " + extractUsername event.Name })
    | Success(NameChanged event) -> 
        jsonOK ({ GenericResponse.Message = "Name changed to: " + extractUsername event.Name })
    | Success(EmailChanged event) -> 
        jsonOK ({ GenericResponse.Message = "Email changed to: " + extractEmail event.Email })
    | Success(PasswordChanged event) -> jsonOK ({ GenericResponse.Message = "Password changed" })
    | Failure(BadRequest f) -> RequestErrors.BAD_REQUEST f
    | Failure NotFound -> genericNotFound

let private runCommand postCommand userId (command : Command) : Types.WebPart = 
    fun httpContext -> async { let! result = postCommand userId command
                               return! mapResultToResponse result httpContext }
let createCommandToWebPartMapper (commandAgent : Guid -> Command -> Async<Result<Event, CQRSFailure>>) 
    (aggregateId : Guid) command : Types.WebPart = 
    fun httpContext -> async { let! result = commandAgent aggregateId command
                               return! mapResultToResponse result httpContext }

let createUser (saveEventToNewStream : Guid -> Event -> Async<unit>) (requestDetails : CreateUserRequest) = 
    let result = 
        CommandHandling.createUserWithAutoGeneration(
            {
                CreateUser.Name = requestDetails.name;
                Email = requestDetails.email;
                Password = requestDetails.password
            })

    match result with
    | Success event ->
        let wrappedEvent = UserCreated event
        saveEventToNewStream event.Id wrappedEvent
        mapResultToResponse (Success wrappedEvent)
    | Failure f ->
        mapResultToResponse (Failure f)

let setName (commandToWebPart : Guid -> Command -> Types.WebPart) (userId : Guid) (requestDetails : SetNameRequest) = 
    commandToWebPart userId (SetName { Id = userId
                                       Name = requestDetails.name })

let setEmail (commandToWebPart : Guid -> Command -> Types.WebPart) (userId : Guid) (requestDetails : SetEmailRequest) = 
    commandToWebPart userId (SetEmail { Id = userId
                                        Email = requestDetails.email })

let setPassword (commandToWebPart : Guid -> Command -> Types.WebPart) (userId : Guid) 
    (requestDetails : SetPasswordRequest) = 
    commandToWebPart userId (SetPassword { Id = userId
                                           Password = requestDetails.password })

(* These methods are just utility methods for debugging etc, services should listen to Event Store events and build their own read models *)
module DiagnosticQueries = 
    type GetUserResponse = 
        { Id : Guid
          Name : string
          Email : string
          CreationTime : string }
    
    type GetUsersResponse = 
        { Users : GetUserResponse seq }
    
    let mapUserToUserResponse (user : User) = 
        { GetUserResponse.Id = user.Id
          Name = extractUsername user.Name
          Email = extractEmail user.Email
          CreationTime = user.CreationTime.ToString() }
    
    let getUser getAggregate id : Suave.Types.WebPart = 
        fun httpContext -> 
            async { 
                let! result = getAggregate id
                return! match fst result with
                        | Some user -> jsonOK (mapUserToUserResponse user) httpContext
                        | None -> genericNotFound httpContext
            }
