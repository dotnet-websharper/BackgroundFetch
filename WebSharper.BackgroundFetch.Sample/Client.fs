namespace WebSharper.BackgroundFetch.Sample

open WebSharper
open WebSharper.JavaScript
open WebSharper.UI
open WebSharper.UI.Notation
open WebSharper.UI.Client
open WebSharper.UI.Templating
open WebSharper.BackgroundFetch

[<JavaScript>]
module Client =
    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let status = Var.Create "Click the button to start downloading..."    
    
    let startBackgroundFetch() = 
        promise {
            try 
                let serviceWorker = JS.Window.Navigator?serviceWorker
                let serviceWorkerController = serviceWorker?controller

                if not(serviceWorkerController) then
                    let serviceWorkerRegister = serviceWorker?register("service-worker.js")
                    do! serviceWorkerRegister |> As<Promise<unit>>
                else
                    let! registration = serviceWorker?ready |> As<Promise<obj>>
                    let bgFetch = registration?backgroundFetch |> As<BackgroundFetchManager>
                    let! bgFetchRegistration = bgFetch.Fetch("file-download", [| "https://th.bing.com/th/id/OIP.kNs4ljTKUnPaBhUzwTScvwHaE8?rs=1&pid=ImgDetMain" |])

                    status := "Downloading in the background..."

                    bgFetchRegistration.AddEventListener("progress", fun (evt: Dom.Event) ->
                        status := $"Downloaded {bgFetchRegistration.Downloaded} of {bgFetchRegistration.DownloadTotal} bytes"
                    )

            with error ->
                status := "Background fetch failed!"
                printfn($"Background Fetch Error: {error.Message}")
        }

    [<SPAEntryPoint>]
    let Main () =

        IndexTemplate.Main()
            .Status(status.V)
            .startBackgroundFetch(fun _ -> 
                async {
                    do! startBackgroundFetch().AsAsync()
                }
                |> Async.Start
            )
            .Doc()
        |> Doc.RunById "main"
