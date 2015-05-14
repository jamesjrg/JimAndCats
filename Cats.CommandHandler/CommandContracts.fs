module Cats.CommandHandler.CommandContracts

open System

type CreateCatRequest = {
    title : string
    Owner : Guid
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
