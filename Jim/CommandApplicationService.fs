namespace Jim.CommandApplicationService

open EventPersistence
open Jim.ApiResponses
open Jim.Domain.CommandsAndEvents
open Jim.ErrorHandling
open Jim.Domain.UserAggregate
open Jim.Domain.IUserRepository
open System

type Message = Command * AsyncReplyChannel<Result<Event, string>>

type CommandAppService(store:IEventStore<Event>, repository:IUserRepository, streamId, initialVersion) =    

    let save expectedVersion events = store.AppendToStream streamId expectedVersion events    

    let agent = MailboxProcessor<Message>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! command, replyChannel = inbox.Receive()
            
            let result = handleCommandWithAutoGeneration command repository
            match result with
            | Success newEvent ->
                do! save version [newEvent]
                handleEvent repository newEvent
                replyChannel.Reply(result)
                return! messageLoop (version + 1)
            | Failure f ->
                replyChannel.Reply(result)
                return! messageLoop version            
            }
        async {          
            return! messageLoop initialVersion
            }      

    member this.runCommand(command:Command) =
        async {
            let! result = agent.PostAndAsyncReply(fun replyChannel -> command, replyChannel)

            match result with
            | Success (UserCreated event) ->
                return OK ( { UserCreatedResponse.Id = event.Id; Message = "User created: " + extractUsername event.Name })
            | Success (NameChanged event) ->
                return OK ( { GenericResponse.Message = "Name changed to: " + extractUsername event.Name })
            | Success (EmailChanged event) ->
                return OK ( { GenericResponse.Message = "Email changed to: " + extractEmail event.Email })
            | Success (PasswordChanged event) ->
                return OK ( { GenericResponse.Message = "Password changed" })
            | Failure f -> return BadRequest ({ GenericResponse.Message = f})
        }