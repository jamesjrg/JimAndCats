module EventPersistence.Tests.IntegrationTests

open EventPersistence

open Xunit
open FsUnit.Xunit

type TestRecordParent =
    | Child1 of Child1
    | Child2 of Child2

and Child1 =
    int * int

and Child2 =
    string * string

[<Fact>]
let ``Should be able to serialize and deserialize nested union events to/from event store``() =
    async {
        let testEvents = [Child1(2, 3); Child2("a", "b")]
        let streamId = "integrationTest"
        let projection (event: TestRecordParent) = printfn "%A" event
        let store = new EventPersistence.EventStore<TestRecordParent>(streamId, projection) :> IEventStore<TestRecordParent>
        do! store.AppendToStream streamId -1 testEvents
        let! events, lastEvent, nextEvent = store.ReadStream streamId -1 500

        events |> should equal testEvents
    }
    