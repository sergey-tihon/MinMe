// Copyright 2018 Fabulous contributors. See LICENSE.md for license.
namespace MinMe

open System
open Fabulous.Core
open Fabulous.DynamicViews
open Xamarin.Forms

module App =

    type IPlatformContract =
        abstract ChooseFile: string[] -> Async<string>
        abstract GetIconForFileType: string -> ImageSource
        abstract ShowProgress: unit -> (string -> unit)
        abstract HideProgress: unit -> unit

    type FileData = {
        Icon: ImageSource
        ContentInfo: Model.FileContentInfo
    }

    type Model =
        | NoFileSelected
        | FileSelected of FileData

    type Msg =
        | OpenFileDialog
        | ChooseFile of fileName:string
        | SetContentInfo of contentInfo:Model.FileContentInfo

    let initModel = NoFileSelected

    let init () =
        //let msg = ChooseFile "/Users/sergey/Downloads/Arks_SEC_Keynote_2018-Final.pptx"
        let msg  = ChooseFile "/Users/sergey/Downloads/SEC_DEEP.FINAL_002.pptx"
        initModel, Cmd.ofMsg msg
        //initModel, Cmd.none

    let update (platform:IPlatformContract) msg model =
        match msg with
        | OpenFileDialog ->
            let cmd = async {
                let! file = platform.ChooseFile [|"pptx"; "docx"|]
                return ChooseFile file
            }
            model, Cmd.ofAsyncMsg cmd
        | ChooseFile file ->
            let cmd  = async {
                let f = platform.ShowProgress()
                let! info = Agents.agent.PostAndAsyncReply(fun replyChannel -> Agents.Analyze(file, replyChannel))
                platform.HideProgress()
                return SetContentInfo info
            }
            model, Cmd.ofAsyncMsg cmd
        | SetContentInfo contentInfo ->
            let fileData = {
                Icon =
                    let ext = IO.Path.GetExtension(contentInfo.FileName).Trim('.')
                    platform.GetIconForFileType ext
                ContentInfo = contentInfo
            }
            FileSelected fileData, Cmd.none



    let view (model: Model) (dispatch: Msg -> unit) =
        match model with
        | NoFileSelected ->
            View.ContentPage(
              // Cannot use StackLayout here because of NRE - https://github.com/xamarin/Xamarin.Forms/issues/4838
              content = View.StackLayout(
                padding = 20.0, verticalOptions = LayoutOptions.Center,
                children = [
                  View.Button(text = "Open file",
                    horizontalOptions = LayoutOptions.Center,
                    command = (fun () -> dispatch OpenFileDialog))
                ])
            )
        | FileSelected file ->
            View.ContentPage(
              content = View.StackLayout(
                //direction = FlexDirection.Column,
                children = [
                  View.Image(
                    source = file.Icon,
                    heightRequest = 50.0,
                    widthRequest = 50.0
                  )
                  View.ScrollView(
                      orientation = ScrollOrientation.Vertical,
                      verticalScrollBarVisibility = ScrollBarVisibility.Always,
                      content = View.TableView(
                        intent = TableIntent.Form,
                        items = [
                            yield ("File", [
                                View.ImageCell(
                                    imageSource = file.Icon,
                                    text = IO.Path.GetFileName(file.ContentInfo.FileName),
                                    detail = file.ContentInfo.ToString()
                                )
                            ])
                            if file.ContentInfo.Videos.Length > 0 then
                                yield (sprintf "Videos (%d)" file.ContentInfo.Videos.Length, [
                                    for text in file.ContentInfo.Videos ->
                                        View.TextCell(text = text)
                                ])
                            if file.ContentInfo.Images.Length > 0 then
                                yield (sprintf "Images (%d)" file.ContentInfo.Images.Length, [
                                    for text in file.ContentInfo.Images ->
                                        View.TextCell(text = text)
                                ])
                        ]
                      )//.FlexGrow(1.0)
                  )
                  View.Button(text = "Open file",
                    horizontalOptions = LayoutOptions.Center,
                    command = (fun () -> dispatch OpenFileDialog))
                ]
              )
            )


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


