self.addEventListener("backgroundfetchsuccess", (event) => {
    event.waitUntil(async function () {
        const registration = event.registration;
        console.log(`Download complete: ${registration.id}`);

        // Show a notification when the download is complete
        self.registration.showNotification("Download Complete", {
            body: `Your file has been downloaded.`,
            icon: "icon.png"
        });
    }());
});

self.addEventListener("backgroundfetchfail", (event) => {
    console.error("Background Fetch Failed:", event.registration.id);
});
