// Public configuration only. No API key is needed for Google/Azure in the browser.
window.APP_CONFIG = Object.freeze({
  apiBaseUrl: "http://localhost:7071/api",
  turnstileSiteKey: "" // Optional public site key; configure the matching server secret.
});
