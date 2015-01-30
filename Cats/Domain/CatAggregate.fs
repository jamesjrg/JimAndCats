module Cats.Domain.CatAggregate

open System
open NodaTime

type Cat = {
    Id: Guid
    CreationTime: Instant
}