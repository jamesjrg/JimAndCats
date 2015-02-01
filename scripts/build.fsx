#r "../packages/Fake/tools/FakeLib.dll"

open Fake   

let title = "JimAndCats"
let authors = [ "James Gregory" ]
let githubLink = "https://github.com/jamesjrg/JimAndCats"

Target "CleanBuild" <| fun _ ->
    CleanDir @"..\MicroCQRS.Common\bin"
    CleanDir @"..\MicroCQRS.Common.Tests\bin"
    CleanDir @"..\Cats\bin"
    CleanDir @"..\Cats.Tests\bin"
    CleanDir @"..\Jim\bin"    
    CleanDir @"..\Jim.Tests\bin"    
    CleanDir @"..\Pledges\bin"
    CleanDir @"..\Pledges.Tests\bin"
    CleanDir @"..\Suave.Extensions\bin"
    CleanDir @"..\Suave.Extensions.Tests\bin"

Target "BuildDebug" <| fun _ ->
    !! @"..\JimAndCats.sln"
    |> MSBuildDebug "" "Build"
    |> Log "MsBuild"
    
Target "BuildRelease" <| fun _ ->
    !! @"..\JimAndCats.sln"
    |> MSBuildRelease "" "Build"
    |> Log "MsBuild"

"CleanBuild" ==> "BuildDebug"
"CleanBuild" ==> "BuildRelease"

RunTargetOrDefault "BuildDebug"
