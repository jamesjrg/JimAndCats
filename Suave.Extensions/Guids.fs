module Suave.Extensions.Guids

open System
open Suave.Types
open Suave.Http
open Suave.Web

//TODO: don't use exceptions
let url_scan_guid (pf:PrintfFormat<'a,'b,'c,'d,string>) (f:Guid -> WebPart) : WebPart =
    Applicatives.url_scan pf (fun idString ->
        match Guid.TryParse(idString) with
        | true, guid -> f guid
        | false, _ -> RequestErrors.BAD_REQUEST (sprintf "Failed to parse id: %s" idString))