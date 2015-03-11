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
            | Failure f -> Failure f |> async.Return            

        member this.Bind(result, func) =
            async {
                let! result = result
                return! this.Bind(result, func)
            }

        member __.Return data =
            data |> Success |> async.Return

        member this.ReturnFrom(result:Result<_,_>) =
            this.Bind(result, Success >> async.Return)

        member this.ReturnFrom(result:Result<_,_> Async) =
            this.Bind(result, Success >> async.Return)

    let resultBuilder = new ResultBuilder()
