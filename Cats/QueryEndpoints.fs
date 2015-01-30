module Cats.QueryEndpoints

open System
open Cats.Domain.ICatRepository
open Suave
open Suave.Types
open Suave.Extensions.Json

type GetCatResponse = {
    Id: Guid
    CreationTime: string
}

let listCats (repository:ICatRepository) = jsonOK "TODO"

let getCat (repository:ICatRepository) id= jsonOK "TODO"