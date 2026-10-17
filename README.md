# Appuntamenti di Natale 2026

Frontend HTML/CSS/Vanilla JS senza build, compatibile con GitHub Pages, e backend C# .NET 10 con Azure Functions v4 isolated. Google Calendar è la fonte delle disponibilità. Le credenziali vengono lette dai **.NET User Secrets su questo PC** durante lo sviluppo e dalle Application Settings in Azure.

## Stato e verifiche

Implementati frontend, endpoint, integrazione REST Google Calendar/OAuth, ricontrollo finale senza lock distribuito, idempotenza persistente tramite ID evento deterministico, email Azure Communication Services, CORS, validazione, limite richieste, punto di integrazione Turnstile e workflow Pages.

L'ambiente Codex ha compilato l'host locale e i servizi condivisi. I test automatici eseguono la logica applicativa e il trasporto Google simulato; non scrivono sul calendario reale e non inviano email. Il runtime Functions e gli adattatori Azure richiedono un restore NuGet, Core Tools e credenziali reali per la verifica completa. In questa sessione DNS/rete NuGet e apertura porte sono limitati; `func` non è installato. Non è stato eseguito alcun deploy. La cartella iniziale non contieneva un repository Git né un remoto.

## Business rules

- Periodo: **07/11/2026 – 05/12/2026 inclusi**, tutti i giorni.
- Fuso: `Europe/Rome`, offset calcolati con `TimeZoneInfo`, senza offset UTC fissi.
- Orari: **10:00–13:00**, **14:00–16:30**.
- Durata: **30 minuti**, candidati ogni 30 minuti, 11 slot giornalieri prima di applicare le occupazioni.
- Pausa minima: **30 minuti tra due appuntamenti**, indipendentemente dall'ordine delle prenotazioni.
- Buffer: **virtuale, calcolato dalle Azure Functions**. Non viene rappresentato come evento Google Calendar e non allunga la durata dell'appuntamento.
- Google Calendar contiene solo l'intervallo reale degli appuntamenti. Ogni evento esistente blocca il relativo intervallo reale, anche se dura più di 30 minuti, è ricorrente, è trasparente o copre un giorno intero.
- Rescheduling: solo manualmente tramite Google Calendar. Nessun login cliente, pannello, endpoint di spostamento o cancellazione.
- I candidati già trascorsi non vengono offerti.

La condizione di conflitto per un candidato `s` è:

```text
s < evento.end + pausa
AND
s + durata + pausa > evento.start
```

Il confronto stretto permette esattamente 30 minuti di separazione. Vengono letti anche gli eventi appena fuori dal range, per applicarne il buffer. Nessuna cache di disponibilità: gli spostamenti manuali sono visibili alla GET successiva.

## Struttura

```text
index.html / booking.html / success.html / privacy.html
assets/css/            reset, variabili visuali, layout responsive
assets/js/             configurazione pubblica, API, disponibilità, form
siteExamples/          riferimenti originali e segnaposto
api/Booking.Api/       Functions e servizi applicativi
api/Booking.LocalHost/ host locale con Google reale o demo, stessi servizi applicativi
api/Booking.Api.Tests/ test xUnit e runner senza socket
scripts/               configurazione secrets e test
.github/workflows/     pubblicazione dei soli file statici
```

`Booking.LocalHost` compila gli stessi sorgenti di configurazione, disponibilità, prenotazione, endpoint, Google e CAPTCHA tramite file collegati. In modalità `--live` usa il servizio Google reale e disabilita le email; in modalità `--demo` sostituisce anche il calendario con un’implementazione in memoria. Non è un'alternativa da pubblicare in Azure.

## Prerequisiti sul Mac

- SDK .NET 10 (presente sul PC).
- Azure Functions Core Tools v4 per avviare le Functions reali.
- Python 3 per servire il frontend.
- Accesso a NuGet, Google Calendar e ai servizi Azure.
- Calendario Google dedicato e progetto Google Cloud con Calendar API abilitata.
- Azure Communication Services Email con mittente verificato, solo per le email reali.

Core Tools, da terminale con Homebrew:

```bash
brew tap azure/functions
brew install azure-functions-core-tools@4
func --version
```

Non sono richiesti Node, npm o framework per il sito. Azurite è facoltativo e può essere installato separatamente o eseguito in un container.

## Credenziali da inserire nei secrets .NET

L'identificativo dello store è `SitoNatale2026-BookingApi`, già impostato nei progetti API e LocalHost. Non occorre `dotnet user-secrets init`. Non incollare valori reali in file versionati o nel frontend.

Dalla root puoi usare il prompt che nasconde i valori:

```bash
./scripts/configure-secrets.sh
```

Oppure per una singola voce:

```bash
dotnet user-secrets set "GOOGLE_CLIENT_ID" "VALORE" --project api/Booking.Api
```

| Nome | Quando serve | Valore da ottenere |
| --- | --- | --- |
| `GOOGLE_CALENDAR_ID` | Google reale | ID del calendario dedicato, da Impostazioni → Integra calendario |
| `GOOGLE_CLIENT_ID` | Google reale | Client ID OAuth 2.0 del progetto Google Cloud |
| `GOOGLE_CLIENT_SECRET` | Google reale | Client secret OAuth corrispondente |
| `GOOGLE_REFRESH_TOKEN` | Google reale | Refresh token dell'account con permesso di modifica sul calendario |
| `EMAIL_CONNECTION_STRING` | Email reali, facoltative | Connessione Azure Communication Services collegata al dominio email |
| `EMAIL_SENDER` | Email reali, facoltative | Indirizzo mittente verificato di ACS |
| `TURNSTILE_SECRET_KEY` | Solo se abiliti CAPTCHA | Chiave privata Cloudflare Turnstile |
| `TURNSTILE_ALLOWED_HOSTNAMES` | Se abiliti CAPTCHA | Host autorizzati separati da virgola, senza schema o path |

`GOOGLE_CALENDAR_ID` e `EMAIL_SENDER` sono identificativi; non sono API key. **Una API key Google non permette di creare prenotazioni**: serve l'autorizzazione OAuth con refresh token.

`AzureWebJobsStorage` è la configurazione dell'host Functions. Non esiste più una connessione Storage applicativa per le prenotazioni. Per questa API con soli trigger HTTP può restare vuota nel file locale iniziale; se Core Tools o il piano Azure richiedono storage, configurarla nel file locale ignorato da Git / Application Settings. Con Azurite usare `UseDevelopmentStorage=true`. L'host carica questa impostazione prima dell'avvio del worker: non affidarsi al caricamento custom dei secrets del worker per le impostazioni dell'host.

Lo script lascia invariati i valori quando premi Invio. I secrets sono un meccanismo locale di sviluppo, non una cassaforte cifrata: in Azure usare Application Settings o riferimenti Key Vault. Non mandarmi le chiavi in chat.

## Configurazione Google Calendar

1. Crea un calendario dedicato agli appuntamenti e imposta `Europe/Rome`.
2. In Google Cloud abilita **Google Calendar API**.
3. Configura la schermata consenso OAuth e un client OAuth con il redirect usato nel flusso di autorizzazione. Nei test aggiungi l'account proprietario ai test user.
4. Autorizza l'account che ha accesso in scrittura al calendario con scope `https://www.googleapis.com/auth/calendar.events`, `access_type=offline` e, quando necessario, `prompt=consent` per ricevere un refresh token.
5. Conserva client ID, secret e refresh token nei secrets sopra. Se usi OAuth Playground, configura le tue credenziali OAuth e il redirect `https://developers.google.com/oauthplayground` nel client appropriato.
6. Inserisci l'ID del calendario, non un URL pubblico né la sua chiave iCal privata.
7. Valuta lo stato dell'app OAuth prima dell'uso operativo: in modalità Testing i refresh token per questi scope possono avere durata limitata. Completa gli adempimenti richiesti da Google per il tuo caso.

Il servizio elenca tutte le pagine, espande gli eventi ricorrenti, ignora le istanze cancellate e supporta eventi giornalieri con fine esclusiva. Crea un solo evento `Prenotazione - {nome}` con dati cliente e booking ID. L'ID pubblico è un UUID indipendente dall'ID Google.

## Avvio locale senza Azure Functions Core Tools

Per provare il sito con il calendario Google reale e gli stessi secrets .NET già configurati:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project api/Booking.LocalHost -- --live
```

In un secondo terminale dalla root:

```bash
python3 scripts/serve-site.py
```

Apri `http://localhost:8080`. Il backend ascolta su `http://127.0.0.1:7071` e usa gli endpoint `/api/availability` e `/api/book` già configurati nel frontend. In modalità `--live` le prenotazioni vengono salvate sul calendario Google reale; **le email sono disabilitate**, anche se presenti nei secrets. Valgono tutte le regole di disponibilità della stagione. Non servono `func` né configurazioni Azure per questo avvio.

L’host locale richiede l’ambiente Development e un solo flag fra `--live` e `--demo`. Non viene avviato alcun browser e non vengono stampati valori dei secrets.

## Avvio alternativo con Azure Functions Core Tools

Dalla root, dopo aver configurato i secrets:

```bash
dotnet restore api/Booking.Api.sln
dotnet build api/Booking.Api/Booking.Api.csproj
cd api/Booking.Api
AZURE_FUNCTIONS_ENVIRONMENT=Development func start --port 7071
```

Il file `local.settings.json` iniziale contiene solo bootstrap e CORS locali, nessuna credenziale; è ignorato da Git. Se manca, copiare `local.settings.example.json` in `local.settings.json`, senza riempire i campi segreti vuoti: i valori reali vanno nei secrets. In sviluppo i secrets hanno precedenza sulle variabili d’ambiente, così i placeholder vuoti del file di esempio non sovrascrivono le credenziali locali. In Azure vengono usate le Application Settings.

In un altro terminale dalla root:

```bash
python3 scripts/serve-site.py
```

Apri `http://localhost:8080`. `assets/js/config.js` contiene l'unico URL API, inizialmente `http://localhost:7071/api`. Non aprire gli HTML via `file://`: i moduli ES richiedono un server HTTP.

Per provare la POST bastano le credenziali Google reali; non serve configurare `AZURE_STORAGE_CONNECTION_STRING`. Le email sono facoltative: la prenotazione rimane confermata se non sono configurate o se il loro invio fallisce.

## Demo senza credenziali

Per controllare il flusso UI senza creare eventi o inviare email:

```bash
ASPNETCORE_ENVIRONMENT=Development dotnet run --project api/Booking.LocalHost -- --demo
```

Poi avvia il server del frontend come sopra. La demo mantiene gli appuntamenti **solo in memoria**: si perdono alla chiusura. Si avvia solo con ambiente Development e flag `--demo`. Non verifica l’adattatore ACS né l'host Functions. Per provare manualmente uno spostamento, usare il calendario reale e richiamare le disponibilità.

## API

### `GET /api/availability?from=2026-11-10&to=2026-11-15`

`from` e `to` sono facoltativi, ISO `yyyy-MM-dd`. Il server limita sempre il risultato alla stagione configurata (inizialmente 29 giorni; configurazione massima di 90 giorni di differenza tra estremi). Intervalli totalmente esterni restituiscono `days: []`; range invertito o date malformate restituiscono 400. Ogni giorno include soltanto gli slot disponibili; giorni pieni includono `slots: []`.

```json
{"timezone":"Europe/Rome","slotDurationMinutes":30,"breakMinutes":30,"days":[{"date":"2026-11-10","slots":[{"start":"2026-11-10T10:00:00+01:00","label":"10:00"}]}]}
```

### `POST /api/book`

Header obbligatori: `Content-Type: application/json` e `Idempotency-Key: <UUID in formato D>`. Riutilizzare la stessa chiave e gli stessi dati dopo un errore di rete. Campi non riconosciuti, inclusi `duration`, `end`, `buffer` e `calendarId`, vengono rifiutati. Il telefono è facoltativo.

```json
{"start":"2026-11-10T10:00:00+01:00","customer":{"name":"Mario Rossi","email":"mario@example.com","phone":"+393331234567"}}
```

```json
{"success":true,"bookingId":"uuid-senza-trattini","start":"2026-11-10T10:00:00+01:00","end":"2026-11-10T10:30:00+01:00","emailSent":true}
```

Errori: `{ "success": false, "error": "CODICE" }`. 400 per validazioni, 409 `SLOT_NOT_AVAILABLE` per slot occupato, 409 `IDEMPOTENCY_CONFLICT` per chiave riutilizzata con dati differenti, 413 per payload oltre 8 KiB, 415 per formato diverso da JSON, 429 per limite richieste, 503 per servizi mancanti/non disponibili. Nessun dettaglio interno o valore segreto nei messaggi al client.

## Concorrenza, idempotenza e limiti operativi

- Per il flusso a basso volume è stato rimosso il lock distribuito: non servono Blob Storage, lease o `AZURE_STORAGE_CONNECTION_STRING` per le prenotazioni.
- La POST rilegge Calendar immediatamente prima della creazione, rivalida slot e pausa e restituisce 409 quando trova un conflitto già presente. Durata e calendario rimangono determinati dal server.
- Il controllo e l'inserimento sono operazioni separate: due richieste contemporanee con chiavi diverse possono entrambe prenotare lo stesso slot o slot adiacenti. Questa possibilità è accettata; gli eventuali conflitti vengono gestiti manualmente contattando il cliente e modificando Calendar.
- Rimangono il blocco del doppio invio nel browser e l'idempotenza tramite ID evento deterministico e fingerprint privato dei dati. Un retry con la stessa chiave recupera l'evento creato, anche se la risposta originale è andata persa.
- Un retry recuperato restituisce conservativamente `emailSent: false`; non ripete l'email. Non è implementata una coda email/outbox: un arresto dopo Calendar e prima dell'invio può lasciare la conferma email non inviata.
- Il limiter è per IP e istanza: 60 GET/minuto e 10 POST/minuto. Su più istanze aggiungere un limite condiviso al gateway/WAF. L'IP non deriva da header client non verificati; dietro proxy controllare l'identità trasmessa dall'infrastruttura. CORS non è autenticazione né protezione dai bot.
- I dati testuali vengono limitati e rifiutano caratteri di controllo; HTML cliente non viene mai interpretato dal frontend. Timeout API e payload sono limitati. I log applicativi non riportano dati cliente, credenziali o corpi delle risposte dei provider.
- In browser l'invio è bloccato durante la richiesta e, in caso di esito incerto, i dati e la chiave sono conservati in sessione per il recupero. Dopo successo si conserva solo il riepilogo senza dati cliente. Usare la stessa scheda per recuperare una richiesta incerta.

## Impostazioni

| Chiave | Default / utilizzo |
| --- | --- |
| `BOOKING_TIME_ZONE` | `Europe/Rome` |
| `BOOKING_START_DATE`, `BOOKING_END_DATE` | `2026-11-07`, `2026-12-05` |
| `BOOKING_MORNING_START`, `BOOKING_MORNING_END` | `10:00`, `13:00` |
| `BOOKING_AFTERNOON_START`, `BOOKING_AFTERNOON_END` | `14:00`, `16:30` |
| `BOOKING_DURATION_MINUTES`, `BOOKING_BREAK_MINUTES` | `30`, `30` |
| `ALLOWED_ORIGINS` | `http://localhost:8080,http://127.0.0.1:8080`; in produzione impostare solo domini autorizzati |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated`, configurazione host |
| `AzureWebJobsStorage` | connessione host, quando richiesta dal runtime/piano |

Le altre chiavi sono nella tabella credenziali e nel file `local.settings.example.json`. In produzione **non** viene letto lo store locale User Secrets.

### CAPTCHA facoltativo

L'integrazione Turnstile è già predisposta su client e server ma disabilitata. Per attivarla: impostare `turnstileSiteKey` (pubblica) in `assets/js/config.js`, `TURNSTILE_SECRET_KEY` nei secrets/Application Settings e `TURNSTILE_ALLOWED_HOSTNAMES`. Il server verifica successo, action `booking` e hostname autorizzato. Aggiornare l'informativa privacy quando si abilita il provider. Non impostare soltanto il secret senza la corrispondente configurazione frontend.

## Test

```bash
dotnet test api/Booking.Api.Tests/Booking.Api.Tests.csproj
```

Se il runner VSTest non può aprire socket, eseguire gli stessi metodi xUnit nel processo:

```bash
./scripts/test-in-process.sh
```

Le suite coprono stagione inclusiva, orari, buffer bidirezionale, pausa a pranzo, eventi lunghi/fuori orario/intera giornata, spostamenti, ora legale, passato, payload/validazione, conflitto finale e rifiuto di slot già prenotati, idempotenza, errore email, CORS, preflight, rate limit, payload e paginazione Google. Verificano esplicitamente che venga creato un solo intervallo reale di 30 minuti.

### Prova reale di creazione su Google Calendar

Il runner dedicato usa il servizio Google reale e carica direttamente i .NET User Secrets dello stesso progetto, senza stampare valori, token o risposte dei provider. Non richiede Core Tools e non apre porte HTTP.

```bash
dotnet run --project api/Booking.IntegrationTests -- --create 2026-10-15 10:00
```

Il comando crea sul calendario configurato `Prenotazione - TEST INTEGRAZIONE`, il **15 ottobre 2026 dalle 10:00 alle 10:30 Europe/Rome**. Non invia email o inviti. Prima controlla occupazioni e pausa virtuale; dopo la creazione rilegge l'evento e verifica inizio, fine e identificativo. Ripetere il comando usa lo stesso ID e non duplica l'evento. L'evento rimane sul calendario per l'ispezione manuale.

È una verifica dell'adattatore Calendar: chiama direttamente il servizio per consentire la data richiesta fuori stagione, senza cambiare le regole produttive della POST `/api/book`. Non verifica Functions o ACS.

Tentativo da Codex del 15/10/2026: compilazione riuscita, chiamata Google bloccata da `NameResolutionError` prima della creazione. Successivamente il test eseguito dal terminale del Mac è riuscito: l’utente ha confermato la comparsa dell’appuntamento nel calendario.

## Deploy Azure (successivo ai test locali)

1. Creare Function App v4 con runtime **.NET 10 isolated** e Storage, in un piano che supporti questa combinazione.
2. Impostare le configurazioni e credenziali sopra in Application Settings/Key Vault, incluso lo storage richiesto dall'host. Non caricare `local.settings.json` o user secrets.
3. Impostare `ALLOWED_ORIGINS` sull'origine del frontend: ad esempio `https://nome.github.io`, **senza** `/repository`. Configurare coerentemente anche il CORS della piattaforma Azure e rimuovere `*`.
4. Verificare prima su calendario e storage di test; pubblicare dalla cartella `api/Booking.Api` con `func azure functionapp publish NOME_FUNCTION_APP`.
5. Cambiare solo `apiBaseUrl` nel frontend con `https://NOME.azurewebsites.net/api`. Nessuna function key privata deve finire nel sito statico.

Il progetto contiene trigger anonimi perché viene chiamato da un frontend pubblico. Abilitare CAPTCHA e limiti infrastrutturali in base al traffico operativo.

## Deploy GitHub Pages

1. Creare un repository Git/remoto e caricare il progetto sul branch `main`.
2. In GitHub → Settings → Pages scegliere **GitHub Actions**.
3. Impostare l'URL API HTTPS in `assets/js/config.js` e autorizzare la relativa origine sul backend.
4. Il workflow `deploy-pages.yml` copia solamente HTML e `assets/`: sorgenti C#, riferimenti e impostazioni locali non vengono pubblicati.

Tutti i link e asset sono relativi: funzionano anche in `https://nome.github.io/nome-repository/`. Nessuna compilazione frontend.

Prima della pubblicazione completare `privacy.html` con titolare/recapiti, base giuridica, conservazione, dettagli dei fornitori e diritti. Servono anche nome definitivo della struttura e contatto da usare per le modifiche manuali. Gli screenshot non includono queste informazioni.

## Documentazione dei provider

- [Azure Functions isolated worker](https://learn.microsoft.com/azure/azure-functions/dotnet-isolated-process-guide)
- [Google Calendar: lista eventi](https://developers.google.com/workspace/calendar/api/v3/reference/events/list)
- [Google Calendar: inserimento eventi](https://developers.google.com/workspace/calendar/api/v3/reference/events/insert)
- [OAuth Google per applicazioni web](https://developers.google.com/identity/protocols/oauth2/web-server)
