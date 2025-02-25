# WebSharper BackgroundFetch API Binding

This repository provides an F# [WebSharper](https://websharper.com/) binding for the [Background Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Background_Fetch_API), enabling seamless background downloading of large resources in WebSharper applications.

## Repository Structure

The repository consists of two main projects:

1. **Binding Project**:

   - Contains the F# WebSharper binding for the Background Fetch API.

2. **Sample Project**:
   - Demonstrates how to use the Background Fetch API with WebSharper syntax.
   - Includes a GitHub Pages demo: [View Demo](https://dotnet-websharper.github.io/BackgroundFetch/).

## Installation

To use this package in your WebSharper project, add the NuGet package:

```bash
   dotnet add package WebSharper.BackgroundFetch
```

## Building

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) installed on your machine.

### Steps

1. Clone the repository:

   ```bash
   git clone https://github.com/dotnet-websharper/BackgroundFetch.git
   cd BackgroundFetch
   ```

2. Build the Binding Project:

   ```bash
   dotnet build WebSharper.BackgroundFetch/WebSharper.BackgroundFetch.fsproj
   ```

3. Build and Run the Sample Project:

   ```bash
   cd WebSharper.BackgroundFetch.Sample
   dotnet build
   dotnet run
   ```

4. Open the hosted demo to see the Sample project in action:
   [https://dotnet-websharper.github.io/BackgroundFetch/](https://dotnet-websharper.github.io/BackgroundFetch/)

## Example Usage

Below is an example of how to use the Background Fetch API in a WebSharper project:

```fsharp
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
    // The templates are loaded from the DOM, so you can edit index.html
    // and refresh your browser without recompiling unless you add or remove holes.
    type IndexTemplate = Template<"wwwroot/index.html", ClientLoad.FromDocument>

    // Variable to track the status of the background fetch operation.
    let status = Var.Create "Click the button to start downloading..."

    let startBackgroundFetch() =
        promise {
            try
                let serviceWorker = JS.Window.Navigator?serviceWorker
                let serviceWorkerController = serviceWorker?controller

                // Check if the service worker is available, if not, register it.
                if not(serviceWorkerController) then
                    let serviceWorkerRegister = serviceWorker?register("service-worker.js")
                    do! serviceWorkerRegister |> As<Promise<unit>>
                else
                    // Retrieve the ready state of the service worker.
                    let! registration = serviceWorker?ready |> As<Promise<obj>>
                    let bgFetch = registration?backgroundFetch |> As<BackgroundFetchManager>

                    // Start fetching the file in the background.
                    let! bgFetchRegistration = bgFetch.Fetch("file-download", [| "https://th.bing.com/th/id/OIP.kNs4ljTKUnPaBhUzwTScvwHaE8?rs=1&pid=ImgDetMain" |])

                    status := "Downloading in the background..."

                    // Listen for progress events to update the download status.
                    bgFetchRegistration.AddEventListener("progress", fun (evt: Dom.Event) ->
                        status := $"Downloaded {bgFetchRegistration.Downloaded} of {bgFetchRegistration.DownloadTotal} bytes"
                    )

            with error ->
                // Handle errors and display a failure message.
                status := "Background fetch failed!"
                printfn($"Background Fetch Error: {error.Message}")
        }

    [<SPAEntryPoint>]
    let Main () =

        IndexTemplate.Main()
            // Bind the status variable to the UI.
            .Status(status.V)
            // Attach event handler to the background fetch button.
            .startBackgroundFetch(fun _ ->
                async {
                    do! startBackgroundFetch().AsAsync()
                }
                |> Async.Start
            )
            .Doc()
        |> Doc.RunById "main"
```

This example demonstrates how to initiate a background fetch request using the Background Fetch API in a WebSharper project.

## Important Considerations

- **Service Worker Requirement**: The Background Fetch API requires a registered Service Worker to function properly.
- **Permissions**: Users must grant permission for background operations when prompted by the browser.
- **Limited Browser Support**: Some browsers may not fully support the Background Fetch API; check [MDN Background Fetch API](https://developer.mozilla.org/en-US/docs/Web/API/Background_Fetch_API) for the latest compatibility information.
