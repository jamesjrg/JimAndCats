namespace Jim.Domain

open System

type IUserRepository =
    abstract member List: unit -> User seq
    abstract member Get: Guid -> User option
    abstract member Put: User -> unit
    abstract member GetByEmail: EmailAddress -> User option