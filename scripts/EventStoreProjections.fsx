open RestSharp

"""
Authorization: Basic YWRtaW46Y2hhbmdlaXQ=
"""

let postProjection name projection =
    let resource = sprintf "http://localhost:2113/projections/continuous?name=%s&emit=yes&checkpoints=yes&enabled=yes" name

let mutable post = new RestRequest(resource, Method.POST)
post.RequestFormat <- DataFormat.Json
request.AddBody(new { accountIds = accountIds });

var response = _client.Post<ManyUserIdsResult>(request);

ThrowOnResponseException(response, HttpStatusCode.OK);

if (response.Data == null)
    throw new UserServiceClientException(response);

return response.Data.UserIdResponses;
