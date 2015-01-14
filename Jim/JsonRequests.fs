module Jim.JsonRequests

open System

type CreateUser = {
    name : string
    email : string
    password : string
    }

type ChangeName = {
    name : string
    }