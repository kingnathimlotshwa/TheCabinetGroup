/*
 * coi-serviceworker v0.1.7 - mdn-content/files/en-us/web/javascript/reference/global_objects/sharedarraybuffer
 * Adds Cross-Origin-Embedder-Policy: require-corp and Cross-Origin-Opener-Policy: same-origin
 * headers via a Service Worker so SharedArrayBuffer (required by SkiaSharp WASM threads) works
 * on GitHub Pages without server configuration.
 *
 * Source: https://github.com/gzuidhof/coi-serviceworker
 */
function coepCredentialless() {
  return true;
}

if (typeof window === 'undefined') {
  // Service Worker scope
  self.addEventListener("install", () => self.skipWaiting());
  self.addEventListener("activate", (event) => event.waitUntil(self.clients.claim()));

  self.addEventListener("fetch", function (event) {
    if (event.request.cache === "only-if-cached" && event.request.mode !== "same-origin") {
      return;
    }

    event.respondWith(
      fetch(event.request)
        .then(function (response) {
          if (response.status === 0) return response;

          const newHeaders = new Headers(response.headers);
          newHeaders.set("Cross-Origin-Embedder-Policy", coepCredentialless() ? "credentialless" : "require-corp");
          newHeaders.set("Cross-Origin-Opener-Policy", "same-origin");

          return new Response(response.body, {
            status: response.status,
            statusText: response.statusText,
            headers: newHeaders,
          });
        })
        .catch((e) => console.error(e))
    );
  });
} else {
  // Main thread — register the service worker
  (async function () {
    if (!window.crossOriginIsolated) {
      if (!("serviceWorker" in navigator)) {
        console.error("coi-serviceworker: Service workers are not supported.");
        return;
      }
      try {
        const registration = await navigator.serviceWorker.register(
          window.location.href.endsWith("/")
            ? "coi-serviceworker.js"
            : "coi-serviceworker.js".replace(/[^/]*$/, "") + "coi-serviceworker.js"
        );
        console.log("coi-serviceworker: Service worker registered:", registration.scope);

        if (!navigator.serviceWorker.controller) {
          console.log("coi-serviceworker: Reloading to activate cross-origin isolation.");
          window.location.reload();
        }
      } catch (e) {
        console.error("coi-serviceworker: Failed to register:", e);
      }
    }
  })();
}
