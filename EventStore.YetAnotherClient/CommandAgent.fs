module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open GenericErrorHandling
open System

let getCommandPoster<'TCommand, 'TEvent, 'TAggregate>
    (getAggregate : Guid-> Async<'TAggregate option>)
    saveAggregate
    (handleCommand:'TCommand -> 'TAggregate option -> Async<Result<'TEvent, CQRSFailure>>)
    (initialVersion:int) =

    let agent = MailboxProcessor<Guid * 'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! aggregateId, command, replyChannel = inbox.Receive()
            
            let! aggregate = getAggregate aggregateId 
            let! result = handleCommand command aggregate
            match result with
            | Success newEvent ->
                do! saveAggregate aggregateId version [newEvent]
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

