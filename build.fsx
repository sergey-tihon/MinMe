#r "nuget: Fake.DotNet.AssemblyInfoFile, 6.0.0"
#r "nuget: Fake.Core.Target, 6.0.0"
#r "nuget: Fake.Tools.Git, 6.0.0"
#r "nuget: Fun.Build, 1.0.5"

open System.IO
open Fun.Build
open Fake.DotNet
open Fake.Tools.Git

let version =
    Changelog.GetLastVersion(__SOURCE_DIRECTORY__)
    |> Option.defaultWith (fun () -> failwith "Version is not found")


pipeline "build" {
    workingDir __SOURCE_DIRECTORY__

    runBeforeEachStage (fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn $"::group::{ctx.Name}")

    runAfterEachStage (fun ctx ->
        if ctx.GetStageLevel() = 0 then
            printfn "::endgroup::")


    stage "Prepare Environment" {
        run "dotnet tool restore"
        run "dotnet paket restore"
    }

    stage "Clear" {
        run "rm -rf ./bin"
        run "mkdir bin"
    }

    stage "AssemblyInfo" {
        run (fun _ ->
            [ AssemblyInfo.Title "MinMe"
              AssemblyInfo.Product "MinMe"
              AssemblyInfo.Description "MinMe helps Office documents to lose extra weight"
              AssemblyInfo.Version version.Version
              AssemblyInfo.FileVersion version.Version ]
            |> AssemblyInfoFile.createCSharp "src/MinMe/Properties/AssemblyInfo.Generated.cs")
    }

    stage "Build" { run "dotnet build MinMe.sln -c Release" }

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

    stage "NuGet" {
        run $"dotnet paket pack bin/ -version %s{version.Version} --release-notes '%s{version.ReleaseNotes}'"
    }

    stage "Publish Windows App" {
        run "dotnet publish src/MinMe.Avalonia -r win-x64 -c Release /p:PublishSingleFile=true"
        run "cp src/MinMe.Avalonia/bin/Release/net8.0/win-x64/publish/MinMe.Avalonia.exe bin/"
    }

    stage "Publish macOS App" {
        run "dotnet publish src/MinMe.Avalonia -r osx-x64 -c Release --self-contained"
        run "cp -r src/MinMe.Avalonia/bin/Release/net8.0/osx-x64/publish/ bin/MinMe.Avalonia.app"
        run "cp src/MinMe.Avalonia/Assets/Info.plist bin/MinMe.Avalonia.app/Contents/"
        run "cp src/MinMe.Avalonia/Assets/AppIcon.icns bin/MinMe.Avalonia.app/Contents/Resources/"
    }

    runIfOnlySpecified
}

tryPrintPipelineCommandHelp ()
