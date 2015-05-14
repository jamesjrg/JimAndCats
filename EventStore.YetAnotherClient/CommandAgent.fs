module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open GenericErrorHandling
open System

let getCommandPoster<'TCommand, 'TEvent, 'TAggregate>
    (repository:GenericRepository<'TAggregate>)
    (handleCommand:'TCommand -> 'TAggregate option -> Async<Result<'TEvent, CQRSFailure>>)
    (streamId:string)    
    (initialVersion:int) = 
    
    let save expectedVersion events = store.AppendToStream streamId expectedVersion events

    let agent = MailboxProcessor<Guid * 'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! aggregateId, command, replyChannel = inbox.Receive()
            
            let! aggregate = repository.Get aggregateId
            let! result = handleCommand command aggregate
            match result with
            | Success newEvent ->
                do! save version [newEvent]
                replyChannel.Reply(result)
                return! messageLoop (version + 1)
            | Failure f ->
                replyChannel.Reply(result)
                return! messageLoop version            
            }
        async {          
            return! messageLoop initialVersion
            }

    fun guid command -> agent.PostAndAsyncReply(fun replyChannel -> guid, command, replyChannel)

