const config = window.APP_CONFIG;
export class ApiError extends Error {
  constructor(code, status = 0) { super(code); this.code = code; this.status = status; }
}
async function call(path, options = {}) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), options.method === "POST" ? 120000 : 25000);
  try {
    const response = await fetch(`${config.apiBaseUrl.replace(/\/$/, "")}/${path}`, { ...options, signal: controller.signal, cache: "no-store", credentials: "omit" });
    let body;
    try { body = await response.json(); } catch { throw new ApiError("INVALID_API_RESPONSE", response.status); }
    if (!response.ok) throw new ApiError(body.error || "API_ERROR", response.status);
    return body;
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new ApiError(error.name === "AbortError" ? "REQUEST_TIMEOUT" : "NETWORK_ERROR");
  } finally { clearTimeout(timeout); }
}
export const getAvailability = () => call("availability");
export const createBooking = (data, key) => call("book", { method: "POST", headers: { "Content-Type": "application/json", "Idempotency-Key": key }, body: JSON.stringify(data) });
export function errorMessage(error) {
  const messages = { CALENDAR_NOT_CONFIGURED: "Il calendario non è ancora configurato. Riprova più tardi.", NETWORK_ERROR: "Connessione non disponibile. Controlla la rete e riprova.", REQUEST_TIMEOUT: "Il servizio non ha risposto in tempo. Riprova con gli stessi dati per verificare la prenotazione.", RATE_LIMITED: "Hai effettuato molte richieste. Attendi un minuto e riprova.", INVALID_EMAIL: "Controlla l’indirizzo email.", INVALID_PHONE: "Controlla il numero di telefono.", CAPTCHA_REQUIRED: "Completa la verifica antispam.", CAPTCHA_INVALID: "Ripeti la verifica antispam.", INVALID_SLOT: "Questo orario non è più prenotabile. Aggiorna le disponibilità." };
  return messages[error.code] || "Il servizio non è al momento disponibile. Riprova tra poco.";
}
