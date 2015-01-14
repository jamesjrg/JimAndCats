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
*)

let mapJsonAsyncFromPostData (f: 'a -> Async<'b>): Types.WebPart =
    fun httpContext ->
        async {
            let bytesAsString = Encoding.UTF8.GetString(httpContext.request.raw_form);

            let maybeRequestJson = 
                try
                    Some (JsonConvert.DeserializeObject<'a>(bytesAsString))
                with
                | e -> None

            match maybeRequestJson with
            | Some requestJson ->
                let! response = f requestJson
                let responseJson = JsonConvert.SerializeObject(response)
                return! ((Successful.OK responseJson) >>= (Writers.set_mime_type "application/json")) httpContext
            | None -> return! RequestErrors.BAD_REQUEST ("Could not parse JSON: " + bytesAsString) httpContext
        }

let mapJsonAsync (f: 'a -> Async<'b>) : Types.WebPart =
    choose
        [
            ParsingAndControl.parse_post_data >>= mapJsonAsyncFromPostData f
            RequestErrors.BAD_REQUEST "Unable to parse post data"
        ]