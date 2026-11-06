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
    /// Inline helper: the `?` operator packs multiple arguments into a single array,
    /// which breaks `addEventListener(event, handler)`. This emits the call as-is.
    [<Inline "$0.addEventListener($1, $2)">]
    let private addEventListener (target: obj) (eventName: string) (handler: Dom.Event -> unit) : unit =
        X<unit>

    // The templates are loaded from the DOM, so you just can edit index.html
    // and refresh your browser, no need to recompile unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    let status = Var.Create "Click “Start download” to begin."
    let progress = Var.Create ""

    let mutable activeRegistration : BackgroundFetchRegistration =
        As<BackgroundFetchRegistration> null

    /// Registers the service worker (if needed) and waits until it is ready.
    let getServiceWorkerRegistration () =
        promise {
            let container = JS.Window.Navigator?serviceWorker
            if isNull container then
                status := "Service workers are not supported in this browser."
                return None
            else
                let! _ = container?register("service-worker.js") |> As<Promise<obj>>
                let! registration = container?ready |> As<Promise<obj>>
                return Some registration
        }

    /// Receives lifecycle messages posted by the service worker.
    let listenForServiceWorkerMessages () =
        let container = JS.Window.Navigator?serviceWorker
        if not (isNull container) then
            addEventListener container "message" (fun (evt: Dom.Event) ->
                let data = evt?data
                if not (isNull data) then
                    let messageType = data?``type`` |> As<string>
                    match messageType with
                    | "backgroundfetchsuccess" ->
                        status := "Background fetch completed successfully."
                        let downloaded = data?downloaded |> As<float>
                        let total = data?downloadTotal |> As<float>
                        progress := sprintf "100%% — %d / %d bytes" (int downloaded) (int total)
                    | "backgroundfetchfail" ->
                        status := "Background fetch failed."
                        progress := ""
                    | "backgroundfetchabort" ->
                        status := "Background fetch aborted."
                        progress := ""
                    | _ -> ()
            )

    /// Starts a background download of the sample file and reports progress.
    let startBackgroundFetch () =
        promise {
            try
                let! registration = getServiceWorkerRegistration ()
                match registration with
                | None -> ()
                | Some registration ->
                    let bgFetch = registration?backgroundFetch |> As<BackgroundFetchManager>
                    if isNull bgFetch then
                        status := "Background Fetch API is not available in this browser."
                    else
                        let options =
                            BackgroundFetchOptions(
                                Title = "WebSharper background fetch demo",
                                DownloadTotal = 1500000.0
                            )
                        let! bgFetchRegistration =
                            bgFetch.Fetch("websharper-demo", [| "sample-data.txt" |], options)
                        activeRegistration <- bgFetchRegistration
                        status := "Download started in the background…"
                        // Report the state we already have: the download may have
                        // completed before the listener below gets attached.
                        let initialTotal = bgFetchRegistration.DownloadTotal
                        let initialDownloaded = bgFetchRegistration.Downloaded
                        progress :=
                            if initialTotal > 0.0 then
                                sprintf "%.0f%% — %d / %d bytes"
                                    (initialDownloaded / initialTotal * 100.0)
                                    (int initialDownloaded)
                                    (int initialTotal)
                            else
                                "0%"
                        bgFetchRegistration.AddEventListener("progress", fun (_: Dom.Event) ->
                            let downloaded = bgFetchRegistration.Downloaded
                            let total = bgFetchRegistration.DownloadTotal
                            let pct = if total > 0.0 then downloaded / total * 100.0 else 0.0
                            progress :=
                                sprintf "%.0f%% — %d / %d bytes" pct (int downloaded) (int total)
                        )
            with error ->
                status := "Background fetch failed: " + error.Message
        }

    /// Aborts the active background download.
    let cancelBackgroundFetch () =
        promise {
            if isNull activeRegistration then
                status := "No active download to cancel."
            else
                let! _ = activeRegistration.Abort()
                activeRegistration <- As<BackgroundFetchRegistration> null
                status := "Download cancelled."
                progress := ""
        }

    [<SPAEntryPoint>]
    let Main () =

        listenForServiceWorkerMessages ()

        IndexTemplate.Main()
            .Status(status.V)
            .Progress(progress.V)
            .startBackgroundFetch(fun _ ->
                async {
                    do! startBackgroundFetch().AsAsync()
                }
                |> Async.StartImmediate
            )
            .cancelBackgroundFetch(fun _ ->
                async {
                    do! cancelBackgroundFetch().AsAsync()
                }
                |> Async.StartImmediate
            )
            .Doc()
        |> Doc.RunById "main"
