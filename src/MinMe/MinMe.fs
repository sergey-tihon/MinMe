// Copyright 2018 Fabulous contributors. See LICENSE.md for license.
namespace MinMe

open System
open System.Diagnostics
open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms

module App = 

    type IPlatformContract =
        abstract ChooseFile: string[] -> Async<string>
        abstract GetIconForFileType: string -> ImageSource

    type FileData = {
        Icon: ImageSource
        ContentInfo: OpenXml.FileContentInfo
    }

    type Model = { 
        File: FileData option
    }

    type Msg = 
        | OpenFileDialog
        | ChooseFile of fileName:string

    let initModel = { 
        File = None; 
    }

    let init () = initModel, Cmd.none

    let update (platform:IPlatformContract) msg model =
        match msg with
        | OpenFileDialog -> 
            let cmd = async {
                let! file = platform.ChooseFile [|"pptx"; "docx"|]
                return ChooseFile file
            }
            model, Cmd.ofAsyncMsg cmd
        | ChooseFile file ->
            let fileData = {
                Icon = 
                    let ext = IO.Path.GetExtension(file).Trim('.')
                    platform.GetIconForFileType ext
                ContentInfo = OpenXml.processFile file
            } 
            { model with File = Some fileData }, Cmd.none

    

    let view (model: Model) (dispatch: Msg -> unit) =
        View.ContentPage(
          content = View.StackLayout(
            padding = 20.0, verticalOptions = LayoutOptions.Center,
            children = [ 
                //View.Image(source = model.Icon)
                //View.ProgressBar(progress = 0.3)
                View.Button(text = "Open file",
                    horizontalOptions = LayoutOptions.Center, 
                    command = (fun () -> dispatch OpenFileDialog))
                View.TableView(
                    intent = TableIntent.Form,
                    minimumHeightRequest = 200.0,
                    items = [
                        ("File", [
                            match model.File with
                            | Some (x) ->
                                yield View.ImageCell(
                                    imageSource = x.Icon,
                                    text = IO.Path.GetFileName(x.ContentInfo.FileName),
                                    detail = x.ContentInfo.ToString()
                                )
                            | None -> ()
                        ])
                    ]
                )
            ]))

    // Note, this declaration is needed if you enable LiveUpdate
    let program platform = Program.mkProgram init (update platform) view

type App (platform: App.IPlatformContract) as app = 
    inherit Application ()

    let runner = 
        App.program platform
#if DEBUG
        |> Program.withConsoleTrace
#endif
        |> Program.runWithDynamicView app

#if DEBUG
    // Uncomment this line to enable live update in debug mode. 
    // See https://fsprojects.github.io/Fabulous/tools.html for further  instructions.
    //
    //do runner.EnableLiveUpdate()
#endif    

    // Uncomment this code to save the application state to app.Properties using Newtonsoft.Json
    // See https://fsprojects.github.io/Fabulous/models.html for further  instructions.
#if APPSAVE
    let modelId = "model"
    override __.OnSleep() = 

        let json = Newtonsoft.Json.JsonConvert.SerializeObject(runner.CurrentModel)
        Console.WriteLine("OnSleep: saving model into app.Properties, json = {0}", json)

        app.Properties.[modelId] <- json

    override __.OnResume() = 
        Console.WriteLine "OnResume: checking for model in app.Properties"
        try 
            match app.Properties.TryGetValue modelId with
            | true, (:? string as json) -> 

                Console.WriteLine("OnResume: restoring model from app.Properties, json = {0}", json)
                let model = Newtonsoft.Json.JsonConvert.DeserializeObject<App.Model>(json)

                Console.WriteLine("OnResume: restoring model from app.Properties, model = {0}", (sprintf "%0A" model))
                runner.SetCurrentModel (model, Cmd.none)

            | _ -> ()
        with ex -> 
            App.program.onError("Error while restoring model found in app.Properties", ex)

    override this.OnStart() = 
        Console.WriteLine "OnStart: using same logic as OnResume()"
        this.OnResume()
#endif


