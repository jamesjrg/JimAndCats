module EventStoreProjections

#I @"..\packages\RestSharp\lib\net4"
#r "RestSharp.dll"

open RestSharp

let postProjection name projection =
    let host = "http://localhost:2113"
    let mutable client = new RestClient(host)
    client.Authenticator <- new HttpBasicAuthenticator("admin","changeit");

    let resource = sprintf "/projections/continuous?name=%s&emit=yes&checkpoints=yes&enabled=yes" name

    let mutable request = new RestRequest(resource, Method.POST)
    request.RequestFormat <- DataFormat.Json
    request.AddParameter(@"application/json", projection, ParameterType.RequestBody) |> ignore 
    printfn "Posting to: %s%s" host resource
    let response = client.Execute(request);
    printfn "Status code: %A" response.StatusCode
    printfn "Body: %s" response.Content