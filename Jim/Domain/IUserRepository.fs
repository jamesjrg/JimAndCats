module Jim.Domain.IUserRepository

open Jim.Domain.UserAggregate
open System

type IUserRepository =
    abstract member List: unit -> User seq
    abstract member Get: Guid -> User option
    abstract member Put: User -> unit