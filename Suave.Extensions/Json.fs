module Suave.Extensions.Json

open Newtonsoft.Json
open Suave
open Suave.Http
open Suave.Web
open System.Text

let private tryParseJson (r:Types.HttpRequest) =
  let str = Encoding.UTF8.GetString(r.raw_form);
  try
    Some (JsonConvert.DeserializeObject<'a>(str)), str
  with
  | e -> None, str

let private tryMapJson' (f: 'a -> Types.WebPart): Types.WebPart =
    Suave.Types.request (fun r ->  
      match tryParseJson r with
      | Some request_json, _ -> f request_json
      | None, str -> RequestErrors.BAD_REQUEST ("Could not parse JSON: " + str)
  )

let private serializeObject obj =
    JsonConvert.SerializeObject(obj)

let jsonOK obj = Successful.OK (serializeObject obj) >>= Writers.set_mime_type "application/json"
let jsonBadRequest obj = RequestErrors.BAD_REQUEST (serializeObject obj) >>= Writers.set_mime_type "application/json"
let genericNotFound = RequestErrors.NOT_FOUND "Not found"  

let tryMapJson (f: 'a -> Types.WebPart): Types.WebPart =
  choose
    [
      ParsingAndControl.parse_post_data >>= tryMapJson' f
      RequestErrors.BAD_REQUEST "Unable to parse post data"
    ]

let mimeTypesWithJson =
  Suave.Http.Writers.default_mime_types_map
    >=> (function
    | ".json" -> Suave.Http.Writers.mk_mime_type "application/json" true
    | _ -> None)