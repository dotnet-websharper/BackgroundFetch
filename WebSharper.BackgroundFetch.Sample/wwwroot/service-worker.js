// Service worker for the WebSharper Background Fetch sample.
// The page starts a background fetch through the BackgroundFetchManager; this
// worker receives the lifecycle events and reports them back to the page.

self.addEventListener("install", () => {
    self.skipWaiting();
});

self.addEventListener("activate", (event) => {
    event.waitUntil(self.clients.claim());
});

// Fired when every request in the background fetch has completed.
self.addEventListener("backgroundfetchsuccess", (event) => {
    const registration = event.registration;
    event.waitUntil((async () => {
        const records = await registration.matchAll();
        const summary = [];
        for (const record of records) {
            const response = await record.responseReady;
            summary.push(`${record.request.url}: ${response.status} ${response.statusText}`);
        }
        console.log("Background fetch succeeded:", registration.id, summary);

        const clients = await self.clients.matchAll({ includeUncontrolled: true, type: "window" });
        for (const client of clients) {
            client.postMessage({
                type: "backgroundfetchsuccess",
                id: registration.id,
                downloaded: registration.downloaded,
                downloadTotal: registration.downloadTotal,
            });
        }

        if (self.registration.showNotification) {
            await self.registration.showNotification("Background fetch complete", {
                body: `Downloaded ${registration.downloaded} of ${registration.downloadTotal} bytes.`,
                tag: "websharper-background-fetch",
            });
        }
    })());
});

// Fired when at least one request in the background fetch failed.
self.addEventListener("backgroundfetchfail", (event) => {
    const registration = event.registration;
    console.error(
        "Background fetch failed:",
        registration.id,
        registration.failureReason
    );
    event.waitUntil((async () => {
        const clients = await self.clients.matchAll({ includeUncontrolled: true, type: "window" });
        for (const client of clients) {
            client.postMessage({
                type: "backgroundfetchfail",
                id: registration.id,
                failureReason: registration.failureReason,
            });
        }
    })());
});

// Fired when the user or the application aborts the background fetch.
self.addEventListener("backgroundfetchabort", (event) => {
    const registration = event.registration;
    console.log("Background fetch aborted:", registration.id);
    event.waitUntil((async () => {
        const clients = await self.clients.matchAll({ includeUncontrolled: true, type: "window" });
        for (const client of clients) {
            client.postMessage({
                type: "backgroundfetchabort",
                id: registration.id,
            });
        }
    })());
});

// Fired when the user clicks the background fetch UI.
self.addEventListener("backgroundfetchclick", (event) => {
    event.waitUntil((async () => {
        const clients = await self.clients.matchAll({ includeUncontrolled: true, type: "window" });
        if (clients.length > 0) {
            clients[0].focus();
        } else {
            await self.clients.openWindow("/");
        }
    })());
});
