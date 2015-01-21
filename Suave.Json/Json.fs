module Suave.Json

open Newtonsoft.Json
open Suave
open Suave.Http
open Suave.Web
open System.Text

(*
Suave has its own Json module with a map_json, but it has various issues:
- it doesn't allow for an async mapping function
- it doesn't have any error handling
- it doesn't allow for setting http status codes
- it uses DataContractJsonSerializer and hence requires you put attributes all over your DTOs, and serializes some objects badly
*)

let private defaultJsonParseFail str =
  RequestErrors.BAD_REQUEST ("Could not parse JSON: " + str)

let private defaultPostDataParseFail =
  RequestErrors.BAD_REQUEST "Unable to parse post data"

let private tryParseJson (r:Types.HttpRequest) =
  let str = Encoding.UTF8.GetString(r.raw_form);
  try
    Some (JsonConvert.DeserializeObject<'a>(str)), str
  with
  | e -> None, str

let private doMapJsonAsync (f: 'a -> Async<'b>) (g: 'b -> Types.WebPart) : Types.WebPart =
  fun (http_context:Types.HttpContext) ->
    async {
      match tryParseJson http_context.request with
      | Some request_json, _ ->
        let! response = f request_json
        return! ((g response >>= Writers.set_mime_type "application/json") http_context)
      | None, str -> return! defaultJsonParseFail str http_context
  }

let serializeObject obj =
    JsonConvert.SerializeObject(obj)

let mapJsonAsync (f: 'a -> Async<'b>) (g: 'b -> Types.WebPart): Types.WebPart =
  choose
    [
      ParsingAndControl.parse_post_data >>= doMapJsonAsync f g
      defaultPostDataParseFail
    ]

let mimeTypesWithJson =
  Suave.Http.Writers.default_mime_types_map
    >=> (function
    | ".json" -> Suave.Http.Writers.mk_mime_type "application/json" true
    | _ -> None)