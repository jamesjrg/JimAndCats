module Cats.Domain.CatAggregate

open System
open NodaTime

type PageTitle = PageTitle of string

type Cat = {
    Id: Guid
    Title: PageTitle
    Owner: Guid
    CreationTime: Instant
}