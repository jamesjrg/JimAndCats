module Jim.DataContracts

open System

type CreateUser = {
    name : string
    email : string
    password : string
    }

type ChangeName = {
    id : Guid
    name : string
    }

type ResponseWithIdAndMessage = {    
    id : Guid
    message : string
    }

type ResponseWithMessage = {
    message: string
}