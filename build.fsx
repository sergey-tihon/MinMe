#r @"paket:
source https://nuget.org/api/v2
framework netstandard2.0
nuget FSharp.Core 5.0.0.0
nuget Fake.Core.Target
nuget Fake.Core.ReleaseNotes
nuget Fake.DotNet.Paket
nuget Fake.DotNet.AssemblyInfoFile
nuget Fake.DotNet.Cli
nuget Fake.Tools.Git
nuget Fake.Api.GitHub //"

#if !FAKE
#load "./.fake/build.fsx/intellisense.fsx"
#r "netstandard" // Temp fix for https://github.com/fsharp/FAKE/issues/1985
#endif


// --------------------------------------------------------------------------------------
// FAKE build script
// --------------------------------------------------------------------------------------

open Fake
open Fake.Core
open Fake.Core.TargetOperators
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators
open Fake.Tools
open Fake.Tools.Git

let gitName = "MinMe"
let description = "MinMe helps Office documents to lose extra weight"
let release = ReleaseNotes.load "RELEASE_NOTES.md"

// Targets
Target.create "Clean" (fun _ ->
    Shell.mkdir "bin"
    Shell.cleanDir "bin"
)

Target.create "AssemblyInfo" (fun _ ->
    let fileName = "src/MinMe/Properties/AssemblyInfo.Generated.cs"
    AssemblyInfoFile.createCSharp fileName
      [ AssemblyInfo.Title gitName
        AssemblyInfo.Product gitName
        AssemblyInfo.Description description
        AssemblyInfo.Version release.AssemblyVersion
        AssemblyInfo.FileVersion release.AssemblyVersion ]
)

Target.create "Build" (fun _ ->
    let result = DotNet.exec id "build" "MinMe.sln -c Release"
    if not result.OK
    then failwithf "Build failed: %A" result.Errors
)

Target.create "RestoreData" (fun _ ->
    let rootDataDir = "tests/data/"
    [
        "dotNETConf", "https://github.com/dotnet-presentations/dotNETConf.git"
        "dsyme-fsharp", "https://github.com/dsyme/fsharp-presentations.git"
        "dl-tutorials", "https://github.com/sjchoi86/dl_tutorials_10weeks.git"
    ]
    |> List.iter (fun (repo, cloneUrl) ->
        let dataDir = Path.combine rootDataDir repo
        if not <| System.IO.Directory.Exists dataDir
        then Repository.cloneSingleBranch "" cloneUrl "master" dataDir
    )
)

Target.create "RunTests" (fun _ ->
    DotNet.test (fun options ->
        { options with
            Common = {
                options.Common with
                    Verbosity = Some <| DotNet.Verbosity.Normal
            }
        }) "tests/MinMe.Tests/"
)

Target.create "NuGet" (fun _ ->
    Paket.pack(fun p ->
        { p with
            ToolType = ToolType.CreateLocalTool()
            OutputPath = "bin"
            Version = release.NugetVersion
            ReleaseNotes = String.toLines release.Notes})
)


let appDir (options:DotNet.Options) =
    {options with WorkingDirectory = "src/MinMe.Avalonia" }

Target.create "PublishWin" (fun _ ->
    //dotnet publish -r win-x64 -c release /p:PublishSingleFile=true
    DotNet.exec appDir "publish" "-r win-x64 -c Release /p:PublishSingleFile=true" |> ignore

    Shell.cp "src/MinMe.Avalonia/bin/Release/net6.0/win-x64/publish/MinMe.Avalonia.exe" "bin/"
)

Target.create "PublishMac" (fun _ ->
    // dotnet publish -c Release --self-contained -r osx.10.15-x64
    DotNet.exec appDir "publish" "-r osx-x64 -c Release --self-contained" |> ignore

    // TODO: https://avaloniaui.net/docs/packing/macOS
    let dir = "bin/MinMe.Avalonia.app"
    Shell.rm dir
    Shell.mkdir dir

    let contents = Path.combine dir "Contents"
    Shell.mkdir contents
    Shell.cp "src/MinMe.Avalonia/Assets/Info.plist" contents

    let resources = Path.combine contents "Resources"
    Shell.mkdir resources
    Shell.cp "src/MinMe.Avalonia/Assets/AppIcon.icns" resources

    let macOs = Path.combine contents "macOS"
    Shell.cp_r "src/MinMe.Avalonia/bin/Release/net6.0/osx-x64/publish/" macOs
)

Target.create "PublishWasm" (fun _ ->
    let wasm (options:DotNet.Options) =
        {options with WorkingDirectory = "src/MinMe.WebAssembly" }

    DotNet.exec wasm "publish" "-c Release" |> ignore

    Shell.copyRecursive "src/MinMe.WebAssembly/bin/Release/net6.0/publish/wwwroot" "bin/wasm" true |> Trace.tracefn "%A"

    let index = "bin/wasm/index.html"
    let html = 
        System.IO.File.ReadAllText(index)
            .Replace("<base href=\"/\" />", "<base href=\"/MinMe/\" />")
    System.IO.File.WriteAllText(index, html)
)

Target.create "ReleaseWasm" (fun _ ->
    let cloneUrl = "git@github.com:sergey-tihon/MinMe.git"
    let tempDocsDir = "tmp/gh-pages"
    
    Shell.cleanDir tempDocsDir
    Repository.cloneSingleBranch "" cloneUrl "gh-pages" tempDocsDir

    Repository.fullclean tempDocsDir
    Shell.copyRecursive "bin/wasm" tempDocsDir true |> Trace.tracefn "%A"
    Staging.stageAll tempDocsDir
    Commit.exec tempDocsDir (sprintf "Update generated wasm app for version %s" release.NugetVersion)
    Branches.push tempDocsDir
)

Target.create "All" ignore

// Build order
"Clean"
  ==> "AssemblyInfo"
  ==> "Build"
  ==> "RestoreData"
  ==> "RunTests"
  ==> "NuGet"
  ==> "PublishWin"
  ==> "PublishMac"
  ==> "All"

"Build"
  ==> "PublishWasm"
  ==> "ReleaseWasm"

// start build
Target.runOrDefault "All"
