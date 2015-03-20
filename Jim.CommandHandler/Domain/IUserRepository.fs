namespace Jim.Domain

open System

type IUserRepository =
    abstract member List: unit -> Async<User seq>
    abstract member Get: Guid -> Async<User option>
    abstract member Put: User -> Async<unit>