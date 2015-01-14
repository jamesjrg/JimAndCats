module Jim.ApiResponses

open System

type ResponseWithIdAndMessage = {    
    id : Guid
    message : string
    }

type ResponseWithMessage = {
    message: string
}