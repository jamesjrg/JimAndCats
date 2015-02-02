module EventStoreProjections

#I @"..\packages\RestSharp\lib\net4"
#r "RestSharp.dll"

open RestSharp

let postProjection name projection =
    let host = "http://localhost:2113"
    let client = new RestClient(host)
    let resource = sprintf "/projections/continuous?name=%s&emit=yes&checkpoints=yes&enabled=yes" name

    let mutable request = new RestRequest(resource, Method.POST)
    request.RequestFormat <- DataFormat.Json
    request.AddParameter(@"application\json", projection, ParameterType.RequestBody) |> ignore 
    printfn "Posting to: %s%s" host resource
    client.Execute(request);
