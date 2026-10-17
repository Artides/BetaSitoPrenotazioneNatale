import { getAvailability, createBooking, errorMessage } from "./api.js";
import { dateLabel, renderDays, renderSlots } from "./availability.js";
const $ = id => document.getElementById(id);
let availability, day, slot, page = 0, submitting = false, pending = null, captchaToken = "", widget;
const pageSize = 5;
try { pending = JSON.parse(sessionStorage.getItem("booking-pending") || "null"); } catch { /* storage unavailable */ }
function storePending(value) {
  pending = value;
  try { if (value) sessionStorage.setItem("booking-pending", JSON.stringify(value)); else sessionStorage.removeItem("booking-pending"); } catch { /* In-memory double-submit guard still works. */ }
}
function status(text, error = false) { $("status").textContent = text; $("status").classList.toggle("error", error); }
function drawDays() {
  renderDays($("days"), availability.days.slice(page * pageSize, (page + 1) * pageSize), availability.timezone, day?.date, chooseDay);
  $("previous").disabled = pending !== null || page === 0;
  $("next").disabled = pending !== null || (page + 1) * pageSize >= availability.days.length;
  if (pending) for (const button of $("days").querySelectorAll("button")) button.disabled = true;
}
function chooseDay(value) {
  if (pending || submitting) return;
  day = value; slot = null; drawDays(); drawSlots();
  $("time-section").hidden = false; $("details-section").hidden = true;
  $("selected-day").textContent = dateLabel(day.date, availability.timezone);
}
function drawSlots() { renderSlots($("slots"), day.slots, slot, chooseSlot); }
function chooseSlot(value) {
  if (pending || submitting) return;
  slot = value; drawSlots(); showDetails();
}
function showDetails() {
  $("details-section").hidden = false;
  $("summary").textContent = `${dateLabel(slot.start, availability.timezone)} · ${slot.label}`;
  $("duration").textContent = `Durata: ${availability.slotDurationMinutes} minuti · Orario ${availability.timezone}`;
}
function freeze(value) {
  for (const input of $("booking-form").querySelectorAll("input")) input.disabled = value;
  for (const button of $("slots").querySelectorAll("button")) button.disabled = value;
  drawDays();
}
async function load() {
  $("retry").hidden = true; $("days").setAttribute("aria-busy", "true");
  status("Caricamento delle disponibilità…");
  try {
    availability = await getAvailability(); page = 0; day = null; slot = null;
    const days = availability.days;
    $("period").textContent = days.length ? `${dateLabel(days[0].date, availability.timezone)} – ${dateLabel(days.at(-1).date, availability.timezone)}` : "";
    status(days.some(d => d.slots.length) ? "Scegli un giorno per vedere gli orari disponibili." : "Al momento non ci sono appuntamenti disponibili.");
    $("time-section").hidden = true; $("details-section").hidden = true;
    if (pending) {
      // Recover an ambiguous request even when its slot is now absent from availability.
      slot = { start: pending.data.start, label: new Intl.DateTimeFormat("it-IT", { timeZone: availability.timezone, hour: "2-digit", minute: "2-digit" }).format(new Date(pending.data.start)) };
      $("name").value = pending.data.customer.name; $("email").value = pending.data.customer.email; $("phone").value = pending.data.customer.phone || ""; $("privacy").checked = true;
      showDetails(); freeze(true);
      status("Una richiesta è in attesa di verifica. Premi Conferma per recuperarne l’esito con gli stessi dati.", true);
    }
    drawDays();
  } catch (error) { status(errorMessage(error), true); $("retry").hidden = false; }
  finally { $("days").setAttribute("aria-busy", "false"); }
}
$("previous").addEventListener("click", () => { if (page > 0 && !pending) { page--; drawDays(); } });
$("next").addEventListener("click", () => { if (!pending && (page + 1) * pageSize < availability.days.length) { page++; drawDays(); } });
$("retry").addEventListener("click", load);
$("booking-form").addEventListener("submit", async event => {
  event.preventDefault();
  if (submitting || !slot || (!pending && !event.target.reportValidity())) return;
  if (window.APP_CONFIG.turnstileSiteKey && !captchaToken) { $("form-error").textContent = "Completa la verifica antispam."; return; }
  if (!pending) storePending({ key: crypto.randomUUID(), data: { start: slot.start, customer: { name: $("name").value.trim(), email: $("email").value.trim(), phone: $("phone").value.trim() } } });
  submitting = true; freeze(true); $("submit").disabled = true; $("submit").textContent = "Conferma in corso…"; $("form-error").textContent = "";
  try {
    const result = await createBooking({ ...pending.data, captchaToken: captchaToken || undefined }, pending.key);
    try { sessionStorage.setItem("booking-confirmation", JSON.stringify({ ...result, timezone: availability.timezone })); } catch {
      // A successful booking must remain visible even with browser storage disabled.
      $("details-section").replaceChildren(); const p = document.createElement("p"); p.textContent = `Prenotazione confermata. Codice: ${result.bookingId}. ${dateLabel(result.start, availability.timezone)} · ${slot.label}. Conserva questi dati.`; $("details-section").append(p); storePending(null); return;
    }
    storePending(null); window.location.assign("./success.html");
  } catch (error) {
    if (error.status >= 400 && error.status < 500) {
      storePending(null); freeze(false);
      if (error.code === "SLOT_NOT_AVAILABLE" || error.code === "INVALID_SLOT") {
        await load(); status("Questo orario è stato appena prenotato o non è più disponibile. Scegli un altro orario.", true);
      } else { $("form-error").textContent = errorMessage(error); $("form-error").focus(); }
    } else {
      $("form-error").textContent = `${errorMessage(error)} Premi nuovamente Conferma per recuperare l’esito della stessa richiesta.`;
      $("form-error").focus();
    }
    if (widget !== undefined) { window.turnstile.reset(widget); captchaToken = ""; }
  } finally { submitting = false; $("submit").disabled = false; $("submit").textContent = "Conferma appuntamento →"; }
});
if (window.APP_CONFIG.turnstileSiteKey) {
  window.onTurnstileReady = () => {
    widget = window.turnstile.render("#captcha", { sitekey: window.APP_CONFIG.turnstileSiteKey, action: "booking", callback: token => { captchaToken = token; }, "expired-callback": () => { captchaToken = ""; }, "error-callback": () => { captchaToken = ""; } });
  };
  const script = document.createElement("script"); script.src = "https://challenges.cloudflare.com/turnstile/v0/api.js?onload=onTurnstileReady&render=explicit"; script.async = true; document.head.append(script);
}
load();
