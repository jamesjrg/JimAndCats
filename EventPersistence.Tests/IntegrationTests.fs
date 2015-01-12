module EventPersistence.IntegrationTests

open EventPersistence

open Xunit

type TestRecordParent =
    | Child1 of Child1
    | Child2 of Child2

and Child1 =
    int * int

and Child2 =
    string * string

[<Fact>]
let ``Should be able to serialize and deserialize events to/from event store``() =
    let streamId = "integrationTest"
    let projection (event: TestRecordParent) = printfn "%A" event
    let store = new EventPersistence.EventStore<TestRecordParent>(streamId, projection) :> IEventStore<TestRecordParent>
    store.AppendToStream streamId -1 [Child1(2, 3); Child2("a", "b")] |> Async.RunSynchronously