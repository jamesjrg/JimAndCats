module Jim.Json

open Newtonsoft.Json
open Suave
open Suave.Http
open Suave.Web
open System.Text

(*
Suave has its own Json module with a map_json, but it has various issues:
- it doesn't allow for an async mapping function
- it doesn't have any error handling
- it uses DataContractJsonSerializer and hence requires you put attributes all over your DTOs

NB I've tried to follow Suave's coding conventions in this file
*)

let try_parse_json (r:Types.HttpRequest) =
  let str = Encoding.UTF8.GetString(r.raw_form);
  try
    Some (JsonConvert.DeserializeObject<'a>(str)), str
  with
  | e -> None, str

let json_success json =
  ((Successful.OK (JsonConvert.SerializeObject(json))) >>= (Writers.set_mime_type "application/json"))

let default_json_parse_fail str =
  RequestErrors.BAD_REQUEST ("Could not parse JSON: " + str)

let default_post_data_parse_fail =
  RequestErrors.BAD_REQUEST "Unable to parse post data"

/// Expose function f through a json call; lets you write like
///
/// let app =
///   url "/path"  >>= map_json some_function;
///
let do_map_json f json_parse_fail =
  Types.request(fun r ->
    match try_parse_json r with
    | Some requestJson, _ -> json_success(f requestJson)
    | None, str -> json_parse_fail str)

let do_map_json_async (f: 'a -> Async<'b>) json_parse_fail: Types.WebPart =
  fun http_context ->
    async {
      match try_parse_json http_context.request with
      | Some request_json, _ ->
        let! response = f request_json
        return! json_success response http_context
      | None, str -> return! json_parse_fail str http_context
  }

let parse_then_map_json post_data_parse_fail json_parse_fail (f: 'a -> 'b) : Types.WebPart =
  choose
    [
      ParsingAndControl.parse_post_data >>= do_map_json f json_parse_fail
      post_data_parse_fail
    ]

let parse_then_map_json_async post_data_parse_fail json_parse_fail (f: 'a -> Async<'b>) : Types.WebPart =
  choose
    [
      ParsingAndControl.parse_post_data >>= do_map_json_async f json_parse_fail
      post_data_parse_fail
    ]

let map_json' (f: 'a -> 'b) post_data_parse_fail json_parse_fail : Types.WebPart =
  parse_then_map_json post_data_parse_fail json_parse_fail f

let map_json_async' post_data_parse_fail json_parse_fail (f: 'a -> Async<'b>) : Types.WebPart =
  parse_then_map_json_async post_data_parse_fail json_parse_fail f

let map_json (f: 'a -> 'b) : Types.WebPart =
  parse_then_map_json default_post_data_parse_fail default_json_parse_fail f

let map_json_async (f: 'a -> Async<'b>) : Types.WebPart =
  parse_then_map_json_async default_post_data_parse_fail default_json_parse_fail f

