module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open GenericErrorHandling
open System

let getCommandPoster<'TCommand, 'TEvent, 'TAggregate>
    (store:IEventStore<'TEvent>)
    (handleCommand:'TCommand -> 'TAggregate -> Async<Result<'TEvent, CQRSFailure>>)
    (handleEvent:'TEvent -> 'TAggregate -> 'TAggregate)
    (streamId:string)
    (buildAggregate:Guid)
    (initialVersion:int) = 
    
    let save expectedVersion events = store.AppendToStream streamId expectedVersion events

    let agent = MailboxProcessor<Guid * 'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! aggregateId, command, replyChannel = inbox.Receive()
            
            let! aggregate = buildAggregate
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

    fun command -> agent.PostAndAsyncReply(fun replyChannel -> command, replyChannel)

