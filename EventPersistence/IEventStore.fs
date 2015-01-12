namespace EventPersistence

type IEventStore<'a> =
    abstract member ReadStream : string -> int -> int -> Async<'a list * int * int option>
    abstract member AppendToStream : string -> int -> 'a list -> Async<unit>