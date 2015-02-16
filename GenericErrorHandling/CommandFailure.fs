namespace GenericErrorHandling

type CQRSFailure =
    | BadRequest of string
    | NotFound

