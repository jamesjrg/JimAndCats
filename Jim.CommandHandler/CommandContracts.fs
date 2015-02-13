module Jim.CommandHandler.CommandContracts

open System

type CreateUserRequest = {
    name : string
    email : string
    password : string
    }

type SetNameRequest = {
    name : string
    }

type SetEmailRequest = {
    email : string
    }

type SetPasswordRequest = {
    password : string
    }

type GenericResponse = {
    Message: string
}

type UserCreatedResponse = {
    Id: Guid
    Message: string
}
