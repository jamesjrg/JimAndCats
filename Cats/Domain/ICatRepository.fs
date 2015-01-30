module Cats.Domain.ICatRepository

open Cats.Domain.CatAggregate
open System

type ICatRepository =
    abstract member List: unit -> Cat seq
    abstract member Get: Guid -> Cat option
    abstract member Put: Cat -> unit