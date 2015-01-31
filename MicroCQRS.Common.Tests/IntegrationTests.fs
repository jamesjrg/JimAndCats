module MicroCQRS.Common.Tests.IntegrationTests

open MicroCQRS.Common

open Fuchu
open Swensen.Unquote.Assertions

type TestRecordParent =
    | Child1 of Child1
    | Child2 of Child2

and Child1 =
    int * int

and Child2 =
    string * string

let testCode = 
    async {
        let testEvents = [Child1(2, 3); Child2("a", "b")]
        let streamId = "integrationTest"
        let store = new MicroCQRS.Common.EventStore<TestRecordParent>(streamId) :> IEventStore<TestRecordParent>
        do! store.AppendToStream streamId -1 testEvents
        let! events, lastEvent, nextEvent = store.ReadStream streamId 0 500

        testEvents =? events
    }

[<Tests>]
let test =
    testCase "Should be able to serialize and deserialize nested union events to/from event store" (fun () -> testCode |> Async.RunSynchronously)