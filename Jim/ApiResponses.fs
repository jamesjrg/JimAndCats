module Jim.ApiResponses

open System

type ResponseWithIdAndMessage = {    
    id : Guid
    message : string
    }

type ApiResponse = 
    | ResponseWithIdAndMessage of ResponseWithIdAndMessage
    | ResponseWithMessage of string

type TaggedApiResponse =
    | OK of ApiResponse
    | NotFound
    | BadRequest of ApiResponse