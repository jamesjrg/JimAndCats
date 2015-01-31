namespace MicroCQRS.Common

open System

type ISimpleRepository<'T> =
    abstract member List: unit -> 'T seq
    abstract member Get: Guid -> 'T option
    abstract member Put: Guid -> 'T -> unit