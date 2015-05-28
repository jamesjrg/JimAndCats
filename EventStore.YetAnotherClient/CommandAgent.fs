module EventStore.YetAnotherClient.CommandAgent

open EventStore.YetAnotherClient
open EventStore.ClientAPI
open GenericErrorHandling
open System

let getCommandAgent<'TCommand, 'TEvent, 'TAggregate>
    (getAggregate : Guid-> Async<'TAggregate option>)
    saveEvent
    (applyCommand:'TCommand -> 'TAggregate option -> Async<Result<'TEvent, CQRSFailure>>) =

    let agent = MailboxProcessor<Guid * 'TCommand * AsyncReplyChannel<Result<'TEvent, CQRSFailure>>>.Start <| fun inbox -> 
        let rec messageLoop version = async {
            let! aggregateId, command, replyChannel = inbox.Receive()
            
            let! aggregate = getAggregate aggregateId 
            let! result = applyCommand command aggregate
            match result with
            | Success newEvent ->
                //TODO: need to handle the case that there are multiple agents writing to the same aggregate
                do! saveEvent aggregateId version newEvent
                replyChannel.Reply(result)
                //TODO: is this necessary, or can you just use 0 instead of ExpectedVersion.NoStream (which is defined as -1)?
                let newVersion = if version = ExpectedVersion.NoStream then 1 else version + 1
                return! messageLoop (newVersion)
            | Failure f ->
                replyChannel.Reply(result)
                return! messageLoop version            
            }
        async {          
            return! messageLoop ExpectedVersion.NoStream
            }

    fun guid command -> agent.PostAndAsyncReply(fun replyChannel -> guid, command, replyChannel)

