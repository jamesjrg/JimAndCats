module MicroCQRS.Common.CommandFailure

type CQRSFailure =
    | BadRequest of string
    | NotFound

