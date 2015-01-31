module MicroCQRS.Common.EventStoreConnectionExtensions

open EventStore.ClientAPI

// This module implements AwaitTask for non generic Task
// It should be obsolete as of F# 4 when it will be implemented in FSharp.Core
[<AutoOpen>]
module AsyncExtensions =
    open System
    open System.Threading
    open System.Threading.Tasks
    type Microsoft.FSharp.Control.Async with
        static member Raise(ex) = Async.FromContinuations(fun (_,econt,_) -> econt ex)

        static member AwaitTask (t: Task) =
            let tcs = new TaskCompletionSource<unit>(TaskContinuationOptions.None)
            t.ContinueWith((fun _ -> 
                if t.IsFaulted then tcs.SetException t.Exception
                elif t.IsCanceled then tcs.SetCanceled()
                else tcs.SetResult(())), TaskContinuationOptions.ExecuteSynchronously) |> ignore
            async {
                try
                    do! Async.AwaitTask tcs.Task
                with
                | :? AggregateException as ex -> 
                    do! Async.Raise (ex.Flatten().InnerExceptions |> Seq.head) }

type IEventStoreConnection with
    member this.AsyncConnect() = Async.AwaitTask(this.ConnectAsync())
    member this.AsyncReadStreamEventsForward stream start count resolveLinkTos =
        Async.AwaitTask(this.ReadStreamEventsForwardAsync(stream, start, count, resolveLinkTos))
    member this.AsyncAppendToStream stream expectedVersion events =
        Async.AwaitTask(this.AppendToStreamAsync(stream, expectedVersion, events))