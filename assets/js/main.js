import { dateLabel } from "./availability.js";
const content = document.querySelector("#confirmation-content");
if (content) {
  try {
    const value = sessionStorage.getItem("booking-confirmation");
    if (value) {
      const data = JSON.parse(value); content.replaceChildren();
      const lines = [dateLabel(data.start, data.timezone), new Intl.DateTimeFormat("it-IT", { timeZone: data.timezone, hour: "2-digit", minute: "2-digit" }).format(new Date(data.start)), `Codice prenotazione: ${data.bookingId}`, data.emailSent ? "La conferma è stata inviata anche via email." : "Conserva questo riepilogo: l’email di conferma potrebbe non essere disponibile."];
      for (const line of lines) { const p = document.createElement("p"); p.textContent = line; content.append(p); }
    }
  } catch { /* The static fallback remains visible. */ }
}

