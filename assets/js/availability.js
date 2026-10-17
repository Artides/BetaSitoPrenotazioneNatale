export function dateLabel(date, timezone, options = { weekday: "long", day: "numeric", month: "long" }) {
  return new Intl.DateTimeFormat("it-IT", { ...options, timeZone: timezone }).format(new Date(date.length === 10 ? `${date}T12:00:00Z` : date));
}
export function renderDays(target, days, timezone, selected, onSelect) {
  target.replaceChildren();
  for (const day of days) {
    const button = document.createElement("button"); button.type = "button"; button.className = "day-card";
    button.disabled = day.slots.length === 0; button.classList.toggle("selected", day.date === selected);
    button.setAttribute("aria-pressed", String(day.date === selected));
    const weekday = document.createElement("span"); weekday.textContent = dateLabel(day.date, timezone, { weekday: "long" });
    const date = document.createElement("strong"); date.textContent = dateLabel(day.date, timezone, { day: "numeric", month: "short" });
    const count = document.createElement("small"); count.textContent = day.slots.length ? `${day.slots.length} orari disponibili` : "Nessuna disponibilità";
    button.append(weekday, date, count); button.addEventListener("click", () => onSelect(day)); target.append(button);
  }
}
export function renderSlots(target, slots, selected, onSelect) {
  target.replaceChildren();
  for (const slot of slots) {
    const button = document.createElement("button"); button.type = "button"; button.className = "slot";
    button.textContent = slot.label; button.classList.toggle("selected", slot.start === selected?.start); button.setAttribute("aria-pressed", String(slot.start === selected?.start));
    button.addEventListener("click", () => onSelect(slot)); target.append(button);
  }
}
