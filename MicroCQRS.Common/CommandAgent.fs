module MicroCQRS.Common.CommandAgent

open MicroCQRS.Common
open MicroCQRS.Common.Result
open System

let getCommandPoster<'TCommand, 'TEvent, 'TRepository, 'TCommandFailure> (store:IEventStore<'TEvent>) (repository:'TRepository) (handleCommand:'TCommand -> 'TRepository -> Result<'TEvent, 'TCommandFailure>) (handleEvent:'TRepository -> 'TEvent -> unit) (streamId:string) (initialVersion:int) = 
    let save expectedVersion events = store.AppendToStream streamId expectedVersion events

    let agent = MailboxProcessor<'TCommand * AsyncReplyChannel<Result<'TEvent, 'TCommandFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! command, replyChannel = inbox.Receive()
            
            let result = handleCommand command repository
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