> Aggiornamento approvato il 15/10/2026: per il basso volume di appuntamenti, eliminare il lock distribuito e la dipendenza applicativa da Azure Blob Storage. La sezione 16 e gli altri requisiti sul lock riportati sotto sono superati da questa decisione. Mantenere il ricontrollo Calendar prima della creazione e l’idempotenza; eventuali conflitti tra richieste simultanee saranno gestiti manualmente dalla struttura. `AZURE_STORAGE_CONNECTION_STRING` non è richiesta. L’eventuale `AzureWebJobsStorage` del runtime Functions rimane una configurazione separata.

# Piano di implementazione per Codex

## Obiettivo

Creare un repository contenente:

- sito statico in HTML/CSS/Vanilla JS;
- compatibile direttamente con GitHub Pages;
- backend separato in C# tramite Azure Functions;
- integrazione con Google Calendar;
- endpoint per disponibilità e prenotazione;
- invio email di conferma;
- struttura semplice e facilmente modificabile;
- cartella `siteExamples/` contenente screenshot/reference image che Codex dovrà usare come riferimento visuale.

Non utilizzare framework frontend.

---

## 1. Struttura repository

Creare questa struttura:

```text
/
├── README.md
├── .gitignore
│
├── index.html
├── booking.html
├── success.html
├── privacy.html
│
├── assets/
│   ├── css/
│   │   ├── reset.css
│   │   ├── variables.css
│   │   └── site.css
│   │
│   ├── js/
│   │   ├── config.js
│   │   ├── api.js
│   │   ├── availability.js
│   │   ├── booking.js
│   │   └── main.js
│   │
│   └── images/
│
├── siteExamples/
│   ├── README.md
│   ├── 01-home.png
│   ├── 02-calendar.png
│   ├── 03-booking-form.png
│   └── 04-confirmation.png
│
├── api/
│   ├── Booking.Api.sln
│   │
│   └── Booking.Api/
│       ├── Booking.Api.csproj
│       ├── Program.cs
│       ├── host.json
│       ├── local.settings.example.json
│       │
│       ├── Functions/
│       │   ├── GetAvailability.cs
│       │   └── CreateBooking.cs
│       │
│       ├── Services/
│       │   ├── AvailabilityService.cs
│       │   ├── GoogleCalendarService.cs
│       │   ├── BookingService.cs
│       │   ├── BookingLockService.cs
│       │   └── EmailService.cs
│       │
│       ├── Models/
│       │   ├── BookingRequest.cs
│       │   ├── BookingResponse.cs
│       │   ├── AvailabilityResponse.cs
│       │   ├── AvailableDay.cs
│       │   └── AvailableSlot.cs
│       │
│       ├── Configuration/
│       │   ├── BookingOptions.cs
│       │   ├── GoogleCalendarOptions.cs
│       │   └── EmailOptions.cs
│       │
│       └── Utils/
│           └── BookingIdGenerator.cs
│
└── .github/
    └── workflows/
        └── deploy-pages.yml
```

Il frontend deve vivere direttamente nella root in modo che GitHub Pages possa servirlo senza build step.

Non introdurre npm, Node, React, Vue, Angular o bundler.

---

## 2. `siteExamples`

La cartella:

```text
siteExamples/
```

serve esclusivamente come riferimento visuale per Codex.

Gli screenshot saranno aggiunti successivamente.

Creare inizialmente:

```text
siteExamples/
├── README.md
├── 01-home.png            # placeholder
├── 02-calendar.png        # placeholder
├── 03-booking-form.png    # placeholder
└── 04-confirmation.png    # placeholder
```

Se gli screenshot non esistono ancora, non inventare il design in maniera eccessiva.

Creare invece una UI:

- pulita;
- responsive;
- mobile first;
- semanticamente corretta;
- facilmente adattabile quando verranno aggiunti gli screenshot.

Nel `siteExamples/README.md` specificare:

```text
Gli screenshot presenti in questa cartella rappresentano
il riferimento visuale per il frontend.

Quando vengono aggiunti o modificati:

1. analizzarne layout, spaziature e gerarchia;
2. adattare HTML e CSS senza modificare il comportamento;
3. non copiare elementi non necessari;
4. mantenere accessibilità e responsive design;
5. non inserire librerie frontend salvo esplicita richiesta.
```

---

## 3. GitHub Pages

Il frontend deve essere immediatamente compatibile con GitHub Pages.

Non utilizzare percorsi assoluti come:

```text
/assets/css/site.css
```

Usare invece:

```text
./assets/css/site.css
```

o URL relativi compatibili anche quando il repository viene pubblicato come:

```text
https://username.github.io/nome-repository/
```

Lo stesso vale per:

- script;
- immagini;
- link interni;
- pagine HTML.

Aggiungere inoltre il workflow GitHub Pages:

```text
.github/workflows/deploy-pages.yml
```

per consentire il deploy automatico dal branch `main`.

Il sito non deve richiedere alcuna fase di compilazione.

---

## 4. Configurazione frontend

Creare:

```text
assets/js/config.js
```

con qualcosa di equivalente a:

```js
window.APP_CONFIG = {
    apiBaseUrl: "https://REPLACE_WITH_AZURE_FUNCTION_URL"
};
```

Deve essere l'unico punto in cui configurare l'URL delle Azure Functions.

Non inserire:

- secret;
- Google credentials;
- API private key;
- token;
- connection string.

nel frontend.

---

## 5. Regole di prenotazione

Centralizzare tutte le regole lato server.

Configurazione iniziale:

```text
timezone:
Europe/Rome

booking start:
2026-11-07

booking end:
2026-12-05

opening hours:
10:00 - 13:00
14:00 - 16:30

days:
tutti i giorni

booking duration:
30 minuti

minimum break between bookings:
30 minuti
```

La data finale è inclusiva.

---

## 6. Regola architetturale fondamentale sul buffer

Questa regola è obbligatoria e deve essere considerata parte centrale del design.

**Google Calendar deve contenere solo la durata reale dell'appuntamento, cioè 30 minuti.**

Esempio:

```text
Prenotazione reale:
10:00 -> 10:30
```

L'evento Google Calendar deve quindi essere esclusivamente:

```text
10:00 -> 10:30
```

**Non creare mai un secondo evento Calendar per rappresentare la pausa.**

La pausa aggiuntiva di 30 minuti deve restare esclusivamente una regola virtuale applicativa e deve essere calcolata dalla Azure Function.

Quindi:

```text
Evento Google Calendar:
10:00 -> 10:30

Buffer virtuale:
10:30 -> 11:00

Primo slot successivo disponibile:
11:00
```

Il buffer virtuale deve essere applicato:

- nella GET `/api/availability`;
- nella validazione finale della POST `/api/book`;
- nel controllo anti-doppia-prenotazione;
- nel ricalcolo delle disponibilità dopo una modifica manuale del Calendar.

Questo comportamento è importante perché l'amministratore deve poter spostare manualmente gli appuntamenti direttamente in Google Calendar.

Se, per esempio, un evento viene spostato manualmente da:

```text
10:00 -> 10:30
```

a:

```text
11:30 -> 12:00
```

la successiva chiamata a:

```http
GET /api/availability
```

deve leggere il nuovo intervallo reale dal Calendar e ricalcolare automaticamente:

```text
evento reale:
11:30 -> 12:00

buffer virtuale:
12:00 -> 12:30
```

senza richiedere alcuna modifica manuale a eventi "pausa".

Se l'amministratore modifica manualmente anche la durata dell'evento, per esempio:

```text
10:00 -> 11:00
```

la Function deve considerare l'intero intervallo reale occupato e applicare il buffer virtuale dopo la fine dell'evento:

```text
evento:
10:00 -> 11:00

buffer virtuale:
11:00 -> 11:30
```

La business logic non deve assumere che tutti gli eventi esistenti su Calendar durino esattamente 30 minuti.

---

## 7. Logica degli slot

Gli slot candidati partono ogni 30 minuti.

Per esempio:

```text
10:00
10:30
11:00
11:30
12:00
12:30

14:00
14:30
15:00
15:30
16:00
```

Non generare:

```text
13:00
13:30
16:30
```

perché il periodo `13:00–14:00` è chiuso e una prenotazione che inizia alle `16:30` terminerebbe fuori dall'orario disponibile.

---

## 8. Regola della pausa

Ogni appuntamento dura:

```text
30 minuti
```

e richiede almeno:

```text
30 minuti
```

prima che possa iniziare l'appuntamento successivo.

Esempio:

```text
Prenotazione:
10:00 -> 10:30

Buffer:
10:30 -> 11:00

Prima nuova prenotazione possibile:
11:00
```

Pertanto:

```text
10:00 prenotato
10:30 NON disponibile
11:00 disponibile
```

Analogamente:

```text
14:30 -> 15:00 prenotato

15:00 NON disponibile
15:30 disponibile
```

La logica deve essere applicata indipendentemente dall'ordine in cui vengono effettuate le prenotazioni.

Questo significa che due prenotazioni non possono mai essere adiacenti.

Per esempio, se esiste:

```text
11:00 -> 11:30
```

anche:

```text
10:30 -> 11:00
```

deve risultare non disponibile.

In altre parole, tra la fine di una prenotazione e l'inizio della successiva devono esserci almeno 30 minuti.

---

## 9. Google Calendar

Utilizzare un calendario Google dedicato.

Placeholder configurazione:

```text
GOOGLE_CALENDAR_ID
```

Google Calendar rappresenta la fonte ufficiale delle prenotazioni.

Gli eventi presenti nel calendario devono essere considerati occupati.

La GET deve interrogare Google Calendar e derivare da esso le disponibilità.

Non creare preventivamente eventi per rappresentare gli slot liberi.

Non creare eventi aggiuntivi per rappresentare il buffer.

---

## 10. Modifiche manuali da Calendar

L'amministratore deve poter modificare liberamente le prenotazioni direttamente da Google Calendar.

Non creare nel sito:

- funzionalità di rescheduling;
- pannello cliente;
- login cliente;
- endpoint `reschedule`.

Se un appuntamento viene spostato manualmente nel Calendar:

```text
Google Calendar
      ↓
GET /availability
      ↓
nuova disponibilità
```

La prossima interrogazione deve automaticamente riflettere la modifica, incluso il ricalcolo del buffer virtuale di 30 minuti.

---

## 11. Cancellazioni

Per questa prima versione non implementare il rescheduling.

Predisporre però il codice affinché in futuro possa essere aggiunta una cancellazione tramite `bookingId`.

Non implementarla finché non viene esplicitamente richiesta.

---

## 12. GET `/api/availability`

Endpoint:

```http
GET /api/availability
```

Opzionalmente accettare:

```text
from
to
```

ma limitare sempre l'intervallo effettivo a:

```text
2026-11-07
->
2026-12-05
```

Una richiesta fuori intervallo non deve generare slot fuori dal periodo prenotabile.

Esempio:

```http
GET /api/availability?from=2026-11-10&to=2026-11-15
```

Risposta:

```json
{
  "timezone": "Europe/Rome",
  "slotDurationMinutes": 30,
  "breakMinutes": 30,
  "days": [
    {
      "date": "2026-11-10",
      "slots": [
        {
          "start": "2026-11-10T10:00:00+01:00",
          "label": "10:00"
        },
        {
          "start": "2026-11-10T11:00:00+01:00",
          "label": "11:00"
        }
      ]
    }
  ]
}
```

Fare attenzione all'ora legale.

Utilizzare `Europe/Rome` e librerie/API timezone-aware. Non hardcodare offset UTC.

---

## 13. Algoritmo della GET

Implementare indicativamente:

```text
1. determina range richiesto
2. clamp tra 7 novembre e 5 dicembre
3. genera gli slot candidati ogni 30 minuti
4. recupera gli eventi Google Calendar nel range
5. normalizza gli eventi in Europe/Rome
6. considera l'intervallo reale di ogni evento
7. applica virtualmente 30 minuti di buffer dopo l'evento
8. impedisce anche slot che terminerebbero a meno di 30 minuti
   dall'inizio di un evento successivo
9. elimina gli slot incompatibili
10. restituisce giorni + slot disponibili
```

Non assumere che tutti gli eventi presenti in Calendar durino esattamente 30 minuti.

---

## 14. POST `/api/book`

Endpoint:

```http
POST /api/book
```

Payload:

```json
{
  "start": "2026-11-10T10:00:00+01:00",
  "customer": {
    "name": "Mario Rossi",
    "email": "mario@example.com",
    "phone": "+393331234567"
  }
}
```

Per la prima versione:

- `name` obbligatorio;
- `email` obbligatoria;
- `phone` predisposto ma decidere successivamente se renderlo obbligatorio.

Non accettare dal client:

```text
duration
end
buffer
calendarId
```

Questi valori devono essere determinati esclusivamente dal server.

---

## 15. Validazioni POST

La POST deve verificare che:

- la data sia tra 7 novembre e 5 dicembre;
- l'orario inizi su uno slot valido di 30 minuti;
- l'orario sia dentro le finestre consentite;
- l'appuntamento termini entro la finestra;
- nome presente;
- email valida;
- slot ancora disponibile;
- siano rispettati i 30 minuti di pausa virtuale rispetto agli eventi esistenti.

Il client non è trusted.

Anche se la GET ha appena detto che lo slot è libero, la POST deve verificarlo nuovamente.

---

## 16. Lock anti-doppia-prenotazione

Prima della verifica finale, ottenere un lock distribuito basato sullo slot.

Per esempio:

```text
booking-lock-2026-11-10-10-00
```

Possibile implementazione tramite Azure Blob Lease.

Flusso:

```text
POST /book
     ↓
acquisizione lock
     ↓
ricontrollo Google Calendar
     ↓
ricalcolo buffer virtuale
     ↓
slot ancora libero?
     ↓
creazione evento
     ↓
release lock
```

Se non è più disponibile:

```http
409 Conflict
```

```json
{
  "success": false,
  "error": "SLOT_NOT_AVAILABLE"
}
```

---

## 17. Evento Google Calendar

Creare evento:

```text
Titolo:
Prenotazione - {nome cliente}
```

Periodo:

```text
start
->
start + 30 minuti
```

Descrizione indicativa:

```text
Prenotazione dal sito

Booking ID: XXXXX

Cliente:
Mario Rossi

Email:
mario@example.com

Telefono:
+39...
```

Regola obbligatoria:

```text
NON creare un evento separato per il buffer.
NON estendere l'evento a 60 minuti solo per includere la pausa.
```

L'evento Calendar deve rappresentare solo la durata reale della prenotazione.

Il buffer di 30 minuti deve restare esclusivamente virtuale e calcolato dall'applicazione.

---

## 18. Booking ID

Generare un nostro ID.

Preferire:

```text
ULID
```

oppure UUID.

Non utilizzare l'ID Google Calendar come identificativo pubblico.

Restituire:

```json
{
  "success": true,
  "bookingId": "01J....",
  "start": "2026-11-10T10:00:00+01:00",
  "end": "2026-11-10T10:30:00+01:00"
}
```

---

## 19. Email

Dopo la creazione corretta dell'evento inviare una mail.

Predisporre un'interfaccia:

```csharp
IEmailService
```

con implementazione iniziale pensata per Azure Communication Services.

L'assenza temporanea del servizio email non deve annullare una prenotazione già inserita su Google Calendar.

Flusso:

```text
Calendar event creato
        ↓
prenotazione confermata
        ↓
tentativo invio email
        ↓
se fallisce:
log dell'errore
```

---

## 20. Contenuto email

Email semplice.

Indicativamente:

```text
Prenotazione confermata

Ciao Mario,

la tua prenotazione è confermata.

Data:
10 novembre 2026

Orario:
10:00

Durata:
30 minuti

Per modifiche alla prenotazione contatta direttamente
la struttura.
```

Non fornire link per cambiare autonomamente l'orario.

---

## 21. Frontend

Prevedere inizialmente tre step:

```text
1. scelta giorno
2. scelta orario
3. dati cliente + conferma
```

UX indicativa:

```text
Home
 ↓
Prenota
 ↓
Calendario / giorni disponibili
 ↓
Slot disponibili
 ↓
Nome + email + telefono
 ↓
Riepilogo
 ↓
Conferma
 ↓
Success
```

---

## 22. Disponibilità calendario frontend

Il frontend non deve calcolare autonomamente la disponibilità.

Deve esclusivamente rappresentare ciò che restituisce:

```http
GET /api/availability
```

Quindi non replicare in JS:

- opening hours;
- buffer;
- periodo prenotabile;
- durata;
- logica Google Calendar.

La business logic rimane server-side.

---

## 23. Stato UI

Gestire almeno:

```text
loading

giorno senza disponibilità

slot disponibile

slot selezionato

submit in corso

prenotazione riuscita

slot diventato indisponibile

errore API

errore rete
```

Se la POST ritorna `409`:

```text
Questo orario è stato appena prenotato.
Scegli un altro orario.
```

Poi ricaricare la disponibilità.

---

## 24. Responsive

Il sito deve essere progettato innanzitutto per smartphone.

Breakpoint semplici.

Non utilizzare framework CSS.

Usare:

```css
:root {
    --...
}
```

per colori, spaziature, radius, font-size ecc.

Questo consentirà di adattare rapidamente il design agli screenshot presenti in `siteExamples`.

---

## 25. Accessibilità

Utilizzare:

- HTML semantico;
- `<button>` per azioni;
- `<label>` per i campi;
- focus states;
- contrasto sufficiente;
- navigazione da tastiera;
- `aria-live` per errori e messaggi dinamici.

Non creare div cliccabili quando può essere utilizzato un vero `<button>`.

---

## 26. Sicurezza minima

Prevedere lato API:

- validazione rigorosa;
- CORS limitato ai domini autorizzati;
- rate limiting;
- massimo range interrogabile dalla GET;
- niente secret nel frontend;
- sanitizzazione dei dati inseriti nell'evento;
- log degli errori senza informazioni sensibili;
- protezione anti-double-submit;
- idempotency dove utile.

Predisporre inoltre un punto d'integrazione per CAPTCHA/Turnstile, senza necessariamente abilitarlo nella prima implementazione.

---

## 27. Configurazione Azure

Usare environment variables / Application Settings.

Placeholder:

```text
BOOKING_TIME_ZONE=Europe/Rome

BOOKING_START_DATE=2026-11-07
BOOKING_END_DATE=2026-12-05

BOOKING_MORNING_START=10:00
BOOKING_MORNING_END=13:00

BOOKING_AFTERNOON_START=14:00
BOOKING_AFTERNOON_END=16:30

BOOKING_DURATION_MINUTES=30
BOOKING_BREAK_MINUTES=30

GOOGLE_CALENDAR_ID=
GOOGLE_CLIENT_ID=
GOOGLE_CLIENT_SECRET=
GOOGLE_REFRESH_TOKEN=

AZURE_STORAGE_CONNECTION_STRING=

EMAIL_CONNECTION_STRING=
EMAIL_SENDER=
```

Non committare i valori reali.

Creare:

```text
local.settings.example.json
```

con placeholder.

Aggiungere:

```text
local.settings.json
```

al `.gitignore`.

---

## 28. Test

Aggiungere almeno unit test per `AvailabilityService`.

Casi fondamentali:

```text
nessuna prenotazione
→ tutti gli slot disponibili

10:00 prenotato
→ 10:00 e 10:30 non disponibili

10:30 prenotato
→ 10:00, 10:30 e 11:00 devono essere valutati
  rispettando la pausa minima

12:30 prenotato
→ pausa cade nel periodo di chiusura

14:00 prenotato
→ 14:30 non disponibile

16:00 prenotato
→ ultimo slot occupato

evento manuale 10:00–11:00
→ corretta applicazione buffer virtuale fino alle 11:30

evento spostato manualmente
→ disponibilità aggiornata e buffer ricalcolato

evento Google fuori dagli orari
→ nessun comportamento anomalo

data 6 novembre
→ non prenotabile

data 7 novembre
→ prenotabile

data 5 dicembre
→ prenotabile

data 6 dicembre
→ non prenotabile
```

Un caso importante da testare esplicitamente è:

```text
Google Calendar:
11:00 -> 11:30

Tentativo:
10:30 -> 11:00

Risultato:
NON disponibile
```

perché non esisterebbero 30 minuti di pausa.

Testare inoltre esplicitamente che:

```text
Evento Google Calendar:
10:00 -> 10:30
```

non venga trasformato, duplicato o esteso per rappresentare il buffer.

Il buffer deve esistere solo nella logica applicativa.

---

## 29. README del progetto

Il `README.md` deve spiegare:

```text
- architettura
- prerequisiti
- configurazione Google Calendar
- configurazione Azure
- avvio locale delle Functions
- configurazione frontend
- deploy Azure
- deploy GitHub Pages
- environment variables
- struttura repository
- regole di disponibilità
```

Includere anche una sezione:

```text
## Business rules
```

contenente chiaramente:

```text
Periodo:
07/11/2026 – 05/12/2026 inclusi

Orari:
10:00–13:00
14:00–16:30

Durata:
30 minuti

Pausa minima:
30 minuti tra due appuntamenti

Buffer:
virtuale, calcolato dalle Azure Functions.
Non deve essere rappresentato come evento Google Calendar.

Google Calendar:
contiene solo l'intervallo reale degli appuntamenti.

Rescheduling:
solo manualmente tramite Google Calendar.
```

---

## 30. Principio architetturale

Codex deve mantenere questa separazione:

```text
Frontend
──────────────────────
visualizzazione
form
UX
chiamate HTTP


Azure Functions
──────────────────────
business rules
validazione
availability
booking
security
buffer virtuale


Google Calendar
──────────────────────
source of truth
degli appuntamenti reali
```

Il frontend non deve diventare fonte di verità.

Google Calendar non deve contenere rappresentazioni artificiali della pausa.

---

## 31. Prima milestone

La prima milestone deve essere completamente funzionante anche senza design definitivo:

```text
[ ] repository creato
[ ] GitHub Pages funzionante
[ ] HTML/CSS/JS statico
[ ] siteExamples predisposta
[ ] GET availability funzionante
[ ] integrazione Calendar funzionante
[ ] buffer virtuale 30 minuti funzionante
[ ] nessun evento Calendar creato per il buffer
[ ] POST booking funzionante
[ ] lock contro doppia prenotazione
[ ] creazione evento Calendar di soli 30 minuti
[ ] email abstraction predisposta
[ ] schermata conferma
[ ] configurazione tramite environment variables
[ ] test AvailabilityService
[ ] test modifiche manuali Calendar + ricalcolo buffer
[ ] README
```

Solo dopo questa milestone adattare il design agli screenshot che verranno messi in `siteExamples`.
