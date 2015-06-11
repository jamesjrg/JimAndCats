namespace EventStore.YetAnotherClient

type IEventStore<'a> =
    abstract member ReadStream : string -> int -> int -> Async<'a list * int * int option>
    abstract member AppendToStream : string -> int -> 'a list -> Async<unit>
    abstract member SubscribeToStreamFrom : string -> int -> ('a -> unit) -> unit