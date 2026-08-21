# Reference — tool di memoria tokensave

Questo file è una reference, non va letta sempre: l'agente la consulta solo se ha bisogno
dei dettagli esatti dei parametri (es. la prima volta che chiama uno di questi tool nella sessione).

## tokensave_record_decision

Salva una decisione di design/architettura.

Parametri tipici:
- `decision` (string, obbligatorio) — frase dichiarativa breve, es. "Usare JWT interno per LDAP
  invece di Basic Auth su ogni richiesta"
- `reason` (string, opzionale) — perché, in 1-2 frasi
- `files` (array di string, opzionale) — path coinvolti
- `tags` (array di string, opzionale) — tag brevi e riusabili, minuscoli, senza spazi

Buone pratiche:
- una decisione per chiamata
- `decision` deve essere comprensibile fuori contesto (chi legge tra 3 mesi non ha la conversazione)
- preferire tag stabili e riutilizzati nel progetto (es. sempre "acl", non a volte "permessi" a
  volte "acl" a volte "authz") così `session_recall` con query libera li trova meglio

## tokensave_record_code_area

Marca un path su cui si è lavorato. Incrementa un contatore "touch" e aggiorna last_touched_at.

Parametri tipici:
- `path` (string, obbligatorio) — file o directory

Buone pratiche:
- usarlo per moduli/feature complete, non per ogni singolo file salvato
- preferire directory (es. `src/Acl/`) a singoli file quando il lavoro ha toccato più file coerenti

## tokensave_session_recall

Interroga le decisioni salvate.

Parametri tipici:
- `query` (string, opzionale) — ricerca FTS5; se omessa, ritorna le decisioni più recenti con
  ranking a decadimento esponenziale (half-life 14 giorni: le vecchie decisioni scendono in
  classifica ma non vengono mai eliminate, restano sempre recuperabili)

Buone pratiche:
- usare query con OR per sinonimi quando non si è sicuri della terminologia esatta usata in passato
  (es. `"acl" OR "permission" OR "authorization"`)
- se la query non trova nulla, non significa che la decisione non esista: provare con termini diversi
  prima di assumere che manchi

## tokensave_session_start / tokensave_session_end

`session_start`:
- salva un baseline delle metriche di salute del codice
- ritorna anche `memory_delta`: fino a 5 decisioni recenti + 5 code-area recenti, troncate, per un
  riepilogo economico di "dove eravamo rimasti" senza dover chiamare `session_recall` a vuoto

`session_end`:
- ricalcola le metriche e le confronta col baseline salvato da `session_start`
- mostra delta per dimensione (es. complessità, dead code, ecc.) e se sono migliorate o peggiorate
- se `session_start` non è mai stato chiamato in questa sessione, `session_end` non ha un baseline
  con cui confrontare: in quel caso non chiamarlo, o chiamarlo sapendo che il confronto sarà vuoto

## Differenza importante rispetto agli altri tool tokensave

Tutti gli altri tool (`tokensave_search`, `tokensave_context`, `tokensave_callers`, ecc.) sono
**read-only** e interrogano il grafo del codice estratto dai file sorgenti — rispondono a "cosa c'è
nel codice ora". I cinque tool di questa reference sono gli unici annotati come non-read-only legati
alla memoria: scrivono stato che persiste tra sessioni e non viene mai ricalcolato dal codice. Sono
concettualmente un taccuino, non un indice.
