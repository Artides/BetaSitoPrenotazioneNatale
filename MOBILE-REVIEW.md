# Review mobile · Impeccable

Verifica del 31 ottobre 2026 su home, prenotazione, conferma e privacy.

## Esito

Il percorso verificato mantiene la palette rossa e verde, la tipografia e i controlli del sito. Le foto sostituiscono l'illustrazione iniziale; la conferma mostra giorno, ora e una foto con «Ti aspettiamo!». Nessun problema bloccante osservato nel percorso demo.

| Dimensione | Valutazione | Evidenza e limiti |
| --- | --- | --- |
| Accessibilità | 3/4 | Alt descrittivi, campi etichettati, focus definito e contrasti principali superiori a 4,5:1. VoiceOver non verificato. |
| Prestazioni | 3/4 | WebP con `srcset`, dimensioni esplicite e caricamento differito delle foto secondarie. Nessuna misurazione su rete mobile reale. |
| Coerenza visiva | 3/4 | Colori e variabili condivisi; restano alcuni colori espliciti del CSS preesistente. |
| Responsive | 3/4 | Layout verificati in Safari; manca il controllo su dispositivi fisici. |
| Integrità dell'implementazione | 4/4 | Percorso demo completato e scansione Impeccable senza segnalazioni dopo le correzioni. |
| **Totale** | **16/20 · Buono** | Valutazione limitata alle verifiche descritte. |

## Correzioni effettuate

- Foto responsive nella home, senza modificare gli originali in `foto/`.
- Intestazione mobile su due righe, navigazione corrente riconoscibile e controlli principali di almeno 44 px.
- Titolo senza parole unite quando l'a capo viene nascosto su mobile.
- Pulsanti dei giorni con layout verticale esplicito, altezza minima di 140 px e aspetto nativo disattivato: giorno, data e disponibilità restano dentro il bordo anche a 320 px.
- Campi a 16 px, pulsante di conferma compatto e titoli coerenti tra pagine.
- Conferma semplificata, senza codice prenotazione o messaggi sull'email, con foto e saluto.

## Verifiche eseguite

Safari su macOS, modalità di design reattivo: home a 320, 390, 768 e 1140 px; percorso di prenotazione e conferma a 320/390 px. Verificati scelta del giorno, selezione dell'orario, compilazione, conferma demo e messaggio di connessione non disponibile. La prova ha usato dati fittizi e un calendario in memoria.

Contrasti calcolati su bianco: testo 14,41:1, testo secondario 5,78:1, pulsante principale 5,74:1, etichette verdi 5,81:1. Verificati percorsi delle immagini e diff senza errori di spaziatura.

## Aggiornamento dell’informativa

La bozza di `privacy.html` è stata sostituita con i dati forniti: titolare Giusy Di Tella, contatto `giusyditella@hotmail.it` e cancellazione dei dati delle prenotazioni dal calendario Google entro il 31/12/2026. Sono indicati finalità, base giuridica, servizi utilizzati e diritti.

La cancellazione dal calendario resta un’attività a carico della titolare: il sito non la esegue automaticamente. L’aggiornamento del testo non costituisce una verifica delle condizioni contrattuali o della configurazione degli account dei fornitori.

La verifica usa viewport emulati e interazioni con mouse; non certifica gesti touch, tastiera iOS, VoiceOver o prestazioni su telefoni reali. Non sono stati misurati i Core Web Vitals.
