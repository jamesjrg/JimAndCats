module Jim.JsonRequests

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

type AuthenticateRequest = {
    email: string
    password : string
    }