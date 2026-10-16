#r "nuget: Fake.Tools.Git, 6.2.0"
#r "nuget: Fun.Build, 1.2.0"

open System.IO
open Fun.Build
open Fake.Tools.Git

let version =
    Changelog.GetLastVersion(__SOURCE_DIRECTORY__)
    |> Option.defaultWith (fun () -> failwith "Version is not found")


pipeline "build" {
    workingDir __SOURCE_DIRECTORY__
    envVars [ ("Version", version.Version) ]

    runBeforeEachStage (fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn $"::group::{ctx.Name}")

    runAfterEachStage (fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn "::endgroup::")

    stage "Clear" {
        run "rm -rf ./bin"
        run "mkdir bin"
    }


    stage "Build" { run "dotnet build MinMe.slnx -c Release" }

    stage "Restore Test Data" {
        run (fun _ ->
            let rootDataDir = "tests/data/"

            [ "dotNETConf", "https://github.com/dotnet-presentations/dotNETConf.git", "main"
              "dsyme-fsharp", "https://github.com/dsyme/fsharp-presentations.git", "master"
              "dl-tutorials", "https://github.com/sjchoi86/dl_tutorials_10weeks.git", "master" ]
            |> List.iter (fun (repo, cloneUrl, branch) ->
                let dataDir = Path.Combine(rootDataDir, repo)

                if not <| Directory.Exists dataDir then
                    Repository.cloneSingleBranch "" cloneUrl branch dataDir))
    }

    stage "Test" { run "dotnet test tests/MinMe.Tests -c Release" }

    stage "NuGet" { run "dotnet pack src/MinMe/MinMe.csproj -o bin/" }

    stage "Publish Windows App" {
        run "dotnet publish src/MinMe.Avalonia -r win-x64 -c Release /p:PublishSingleFile=true"
        run "cp src/MinMe.Avalonia/bin/Release/net10.0/win-x64/publish/MinMe.Avalonia.exe bin/"
    }

    stage "Publish macOS App" {
        run "dotnet publish src/MinMe.Avalonia -r osx-arm64 -c Release --self-contained -p:UseAppHost=true"
        run "mkdir -p bin/MinMe.app/Contents/MacOS/"
        run "cp -r src/MinMe.Avalonia/bin/Release/net10.0/osx-arm64/publish/ bin/MinMe.app/Contents/MacOS"
        run "mkdir -p bin/MinMe.app/Contents/Resources/"
        run "cp src/MinMe.Avalonia/Assets/Info.plist bin/MinMe.app/Contents/"
        run "cp src/MinMe.Avalonia/Assets/AppIcon.icns bin/MinMe.app/Contents/Resources/"
    }

    runIfOnlySpecified
}

tryPrintPipelineCommandHelp ()
