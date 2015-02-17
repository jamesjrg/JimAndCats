module Suave.Extensions.Guids

open System
open Suave.Types
open Suave.Http

let urlScanGuid (pf:PrintfFormat<'a,'b,'c,'d,string>) (f:Guid -> WebPart) : WebPart =
    Applicatives.urlScan pf (fun idString ->
        match Guid.TryParse(idString) with
        | true, guid -> f guid
        | false, _ -> RequestErrors.BAD_REQUEST (sprintf "Failed to parse id: %s" idString))