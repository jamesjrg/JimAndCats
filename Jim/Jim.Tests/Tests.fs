module Jim.Tests.EventStoreTests

open Jim.Domain
open Jim.Tests.Specifications
open Xunit

[<Fact>]
let ``Should be able to create a user``() =
    Given []
    |> When ( CreateUser { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } )
    |> Expect [ UserCreated { Name="Bob Holness"; Email="bob.holness@itv.com"; Password="p4ssw0rd" } ]