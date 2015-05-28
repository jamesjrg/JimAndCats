namespace GenericErrorHandling

[<AutoOpen>]
module Result =
    type Result<'a, 'b> = 
        | Success of 'a
        | Failure of 'b

    type ResultBuilder() =
        member __.Bind(result, func) =
            match result with
            | Success s -> func s
            | Failure f -> Failure f        

        member __.Return data =
            data

    let resultBuilder = new ResultBuilder()
