#r "packages/Fake/tools/FakeLib.dll"

open Fake   

let title = "JimAndCats"
let authors = [ "James Gregory" ]
let githubLink = "https://github.com/jamesjrg/JimAndCats"

Target "CleanBuild" <| fun _ ->
    CleanDir @"EventPersistence\bin\Release"
    CleanDir @"Jim\bin\Release"
    CleanDir @"Cats\bin\Release"

Target "Build" <| fun _ ->
    !! "JimAndCats.sln"
    |> MSBuildRelease @"bin\Release" "Build"
    |> Log "MsBuild"

Target "All" DoNothing

"CleanBuild" ==> "Build"

"Build" ==> "All"

RunTargetOrDefault "All"
