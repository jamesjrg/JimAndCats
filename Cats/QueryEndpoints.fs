module Cats.QueryEndpoints

open System
open Cats.Domain.CatAggregate
open MicroCQRS.Common
open Suave.Extensions.Json

type GetCatResponse = {
    Id: Guid
    CreationTime: string
}

type GetCatsResponse = {
    Cats: GetCatResponse seq
}

let mapCatToCatResponse (cat:Cat) =
    {
        GetCatResponse.Id = cat.Id
        CreationTime = cat.CreationTime.ToString()
    } 

let getCat (repository:ISimpleRepository<Cat>) id =
    match repository.Get(id) with
    | Some cat -> jsonOK (mapCatToCatResponse cat)
    | None -> genericNotFound

let listCats (repository:ISimpleRepository<Cat>) =
    let cats = repository.List() |> Seq.map mapCatToCatResponse
    jsonOK {GetCatsResponse.Cats = cats}