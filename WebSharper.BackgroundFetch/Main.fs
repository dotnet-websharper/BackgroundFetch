namespace WebSharper.BackgroundFetch

open WebSharper
open WebSharper.JavaScript
open WebSharper.InterfaceGenerator

module Definition =

    let BackgroundFetchIcons = 
        Pattern.Config "BackgroundFetchIcons" {
            Required = []
            Optional = [
                "src", T<string>
                "sizes", T<string> 
                "type", T<string>
                "label", T<string> 
            ]
        }

    let BackgroundFetchOptions =
        Pattern.Config "BackgroundFetchOptions" {
            Required = []
            Optional = [
                "title", T<string>
                "icons", !| BackgroundFetchIcons 
                "downloadTotal", T<float>
            ]
        }

    let RequestInfo = T<Request> + T<string> 

    let BackgroundFetchMatchOptions =
        Pattern.Config "BackgroundFetchMatchOptions" {
            Required = []
            Optional = [
                "ignoreSearch", T<bool>
                "ignoreMethod", T<bool>
                "ignoreVary", T<bool>
            ]
        }

    let BackgroundFetchRecord =
        Class "BackgroundFetchRecord"
        |+> Instance [
            "request" =? T<Request> // Represents the Request object
            "responseReady" =? T<Promise<Response>> // Promise resolving to Response object
        ]

    let BackgroundFetchRegistration =
        Class "BackgroundFetchRegistration"
        |=> Inherits T<Dom.EventTarget>
        |+> Instance [
            // Read-only properties
            "id" =? T<string>
            "uploadTotal" =? T<float>
            "uploaded" =? T<float>
            "downloadTotal" =? T<float>
            "downloaded" =? T<float>
            "result" =? T<string> // Can be "success", "failure", or empty string
            "failureReason" =? T<string> // Possible values: "", "aborted", "bad-status", "fetch-error", "quota-exceeded", "download-total-exceeded"
            "recordsAvailable" =? T<bool>

            // Methods
            "abort" => T<unit> ^-> T<Promise<bool>> // Resolves to true if aborted
            "match" => RequestInfo?request * !?BackgroundFetchMatchOptions?options ^-> T<Promise<_>>.[BackgroundFetchRecord] // Returns a BackgroundFetchRecord
            "matchAll" => !?RequestInfo?request * !?BackgroundFetchMatchOptions?options ^-> T<Promise<_>>.[!| BackgroundFetchRecord] // Returns an array of BackgroundFetchRecord

            // Events
            "onprogress" =@ T<unit> ^-> T<unit>
            |> ObsoleteWithMessage "Use OnProgress instead"
            "onprogress" =@ T<Dom.Event> ^-> T<unit>
            |> WithSourceName "OnProgress"
        ]

    let BackgroundFetchManager =
        Class "BackgroundFetchManager"
        |+> Instance [
            "fetch" => T<string>?id * (!| RequestInfo + RequestInfo)?requests * !?BackgroundFetchOptions?options 
                        ^-> T<Promise<_>>.[BackgroundFetchRegistration] // Returns a Promise resolving to BackgroundFetchRegistration
            "get" => T<string>?id ^-> T<Promise<_>>[BackgroundFetchRegistration] // Returns a Promise resolving to BackgroundFetchRegistration or undefined
            "getIds" => T<unit> ^-> T<Promise<_>>[!| T<string>] // Returns a Promise resolving to an array of fetch IDs
        ]

    let BackgroundFetchEventOptions = 
        Pattern.Config "BackgroundFetchEventOptions" {
            Required = []
            Optional = [
                "registration", BackgroundFetchRegistration.Type
            ]
        }

    let BackgroundFetchEvent =
        Class "BackgroundFetchEvent"
        |=> Inherits T<Dom.Event> // Inherit from ExtendableEvent
        |+> Static [
            Constructor (T<string>?``type`` * BackgroundFetchEventOptions?options)
        ]
        |+> Instance [
            "registration" =? BackgroundFetchRegistration // Represents the BackgroundFetchRegistration object
        ]

    let UpdateUIOptions = 
        Pattern.Config "UpdateUIOptions" {
            Required = []
            Optional = [
                "icons", BackgroundFetchIcons.Type
                "title", T<string>
            ]
        }

    let BackgroundFetchUpdateUIEvent =
        Class "BackgroundFetchUpdateUIEvent"
        |=> Inherits BackgroundFetchEvent // Inherit from BackgroundFetchEvent
        |+> Static [
            Constructor (T<string>?``type`` * BackgroundFetchEventOptions?options)
        ]
        |+> Instance [
            "updateUI" => !?UpdateUIOptions?options ^-> T<Promise<unit>> // Returns a Promise that resolves when UI is updated
        ]

    let Assembly =
        Assembly [
            Namespace "WebSharper.BackgroundFetch" [
                BackgroundFetchUpdateUIEvent
                UpdateUIOptions
                BackgroundFetchEvent
                BackgroundFetchEventOptions
                BackgroundFetchManager
                BackgroundFetchRegistration
                BackgroundFetchRecord
                BackgroundFetchMatchOptions
                BackgroundFetchOptions
                BackgroundFetchIcons
            ]
        ]

[<Sealed>]
type Extension() =
    interface IExtension with
        member ext.Assembly =
            Definition.Assembly

[<assembly: Extension(typeof<Extension>)>]
do ()
