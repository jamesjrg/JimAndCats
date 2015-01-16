module Jim.ApiResponses

open System

type ResponseWithIdAndMessage = {    
    id : Guid
    message : string
    }

type ResponseWithMessage = {
    message: string
}

type ApiResponse = 
    | ResponseWithIdAndMessage of ResponseWithIdAndMessage
    | ResponseWithMessage of ResponseWithMessage