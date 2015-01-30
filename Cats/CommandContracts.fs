module Cats.CommandContracts

open System

type CreateCatRequest = {
    title : string
    }

type SetTitleRequest = {
    title : string
    }

type GenericResponse = {
    Message: string
}

type CatCreatedResponse = {
    Id: Guid
    Message: string
}
