module Jim.Json

open Newtonsoft.Json
open Suave
open Suave.Http
open Suave.Web
open System.Text

(*
Suave has its own Json module with a map_json, but it has various issues:
- it doesn't allow for an async mapping function
- it doesn't allow you to insert any error handling
- it uses DataContractJsonSerializer and hence requires you put attributes all over your DTOs
*)

//TODO: error handling for parse_post_data and Json.Net serialize/deserialize
let mapJsonAsync (f: 'a -> Async<'b>) : Types.WebPart =
    ParsingAndControl.parse_post_data >>=
    fun httpContext ->
        async {
            let bytesAsString = Encoding.UTF8.GetString(httpContext.request.raw_form);
            let requestJson = JsonConvert.DeserializeObject<'a>(bytesAsString);
            let! response = f requestJson
            let responseJson = JsonConvert.SerializeObject(response)
            return! ((Successful.OK responseJson) >>= (Writers.set_mime_type "application/json")) httpContext
        }