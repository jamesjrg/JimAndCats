module MicroCQRS.Common.CommandFailure

type CommandFailure =
    | BadRequest of string
    | NotFound

