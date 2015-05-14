module Cats.QueryHandler.AppService

open System
open EventStore.YetAnotherClient
open NodaTime
open Suave.Extensions.Json

type PageTitle = PageTitle of string

type Cat = {
    Id: Guid
    Title: PageTitle
    Owner: Guid
    CreationTime: Instant
}

type GetCatResponse = {
    Id: Guid
    CreationTime: string
}

type GetCatsResponse = {
    Cats: GetCatResponse seq
}

let getRepository() = 
    new GenericInMemoryRepository<Cat>()

let mapCatToCatResponse (cat:Cat) =
    {
        GetCatResponse.Id = cat.Id
        CreationTime = cat.CreationTime.ToString()
    } 

let getCat (repository:IGenericRepository<Cat>) id =
    match repository.Get(id) with
    | Some cat -> jsonOK (mapCatToCatResponse cat)
    | None -> genericNotFound

let listCats (repository:IGenericRepository<Cat>) =
    let cats = repository.List() |> Seq.map mapCatToCatResponse
    jsonOK {GetCatsResponse.Cats = cats}