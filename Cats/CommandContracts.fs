module Cats.CommandContracts

open System

type CreateCatRequest = {
    name : string
    email : string
    password : string
    }

type GenericResponse = {
    Message: string
}

type CatCreatedResponse = {
    Id: Guid
    Message: string
}
