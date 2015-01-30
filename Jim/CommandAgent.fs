module Jim.CommandAgent

open EventPersistence
open Jim.Domain.CommandsAndEvents
open Jim.Shared.ErrorHandling
open Jim.Domain.IUserRepository
open System

type Message = Command * AsyncReplyChannel<Result<Event, string>>

let getCommandPoster (store:IEventStore<Event>) (repository:IUserRepository) streamId initialVersion = 
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

    fun command -> agent.PostAndAsyncReply(fun replyChannel -> command, replyChannel)