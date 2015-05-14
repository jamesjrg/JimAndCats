namespace EventStore.YetAnotherClient

open System

type IGenericRepository<'T> =
    abstract member List: unit -> Async<'T seq>
    abstract member Get: Guid -> Async<'T option>
    abstract member Put: Guid -> 'T -> Async<unit>