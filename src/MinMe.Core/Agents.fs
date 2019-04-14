module MinMe.Agents

open System.IO
open MinMe.Model
open MinMe.OpenXml

type Message =
    | Analyze of fileName:string * AsyncReplyChannel<FileContentInfo>

let private processFile fileName =
    use fs = File.Open(fileName, FileMode.Open)
    let contentInfo = {
        FileContentInfo.Default with
            FileName = fileName
            FileSize = fs.Length
    }
    // TODO: Identify by content intead of extension
    match Path.GetExtension(fileName).ToLowerInvariant() with
    | ".pptx" -> contentInfo |> PowerPoint.processPowerPointStream fs
    | ".docx" -> contentInfo |> Word.processWordStream fs
    | ext -> failwithf "File type '%s' is not supported" ext


let agent = new MailboxProcessor<Message>(fun inbox ->
    let rec loop() = async {
        let! message = inbox.Receive()
        match message with
        | Analyze(fileName, replyChannel) ->
            let info = processFile fileName
            replyChannel.Reply(info)
        do! loop()
    }
    loop())

agent.Start()


let analyze fileName=
    agent.PostAndAsyncReply(fun reply -> Analyze(fileName, reply))
    |> Async.StartAsTask