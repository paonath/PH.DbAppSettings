---
name: tokensave-memory-bridge
description: Usa la memoria cross-session di tokensave (record_decision, record_code_area, session_recall, session_start/session_end) per non rispiegare scelte architetturali ad ogni nuova sessione. Attivala a inizio sessione di lavoro su un progetto indicizzato da tokensave, dopo aver preso una decisione di design/architettura importante, prima di toccare un'area di codice non battuta di recente, o quando l'utente chiede "cosa avevamo deciso su...", "perché abbiamo fatto così", "riassumi lo stato del progetto", "salva questa decisione", "cosa ricordi di questo modulo".
tools: mcp_tokensave_*
---

# Tokensave Memory Bridge

## Perché questa skill

Il server MCP `tokensave` espone tool di codice (search, context, callers, ecc.) **e** tre tool di
memoria cross-sessione, persistiti in `.tokensave/tokensave.db` dentro il progetto:

| Tool                          | Scopo                                                                      |
| ----------------------------- | --------------------------------------------------------------------------- |
| `tokensave_record_decision`   | Salva una decisione di design/architettura (motivo, file coinvolti, tag)    |
| `tokensave_record_code_area`  | Marca un'area di codice toccata (contatore + timestamp ultimo tocco)        |
| `tokensave_session_recall`    | Interroga (FTS5) le decisioni salvate; ranking a decadimento se senza query |
| `tokensave_session_start`     | Salva baseline metriche di salute + ritorna un "memory_delta" recente       |
| `tokensave_session_end`       | Confronta le metriche correnti col baseline, mostra cosa è migliorato       |

A differenza di Claude Code (dove `CLAUDE.md` istruisce l'agente automaticamente a usarli),
Antigravity non ha un meccanismo equivalente nativo per questi tool: questa skill è il "CLAUDE.md"
di Antigravity per tokensave. Senza questa skill, l'agente userà solo i tool di lettura del codice
(`tokensave_search`, `tokensave_context`, ecc.) e ignorerà la memoria persistente.

**Importante — cosa NON è questa memoria**: non è memoria personale dell'utente né uno storico
della conversazione. È legata al *progetto* (alla cartella, tramite `.tokensave/tokensave.db`), va
scritta esplicitamente (nessuna estrazione automatica di "fatti" dalla chat), e riguarda decisioni
tecniche sul codice — non preferenze personali, dati sensibili o informazioni non legate al progetto.
Non salvarci segreti, credenziali, o dati personali dell'utente.

## Quando attivarsi

1. **Inizio sessione** su un progetto che ha una cartella `.tokensave/` nella root (verificalo con
   `ls .tokensave` o tramite `tokensave_status`). Se non esiste, la skill non si applica: suggerisci
   `tokensave init` solo se l'utente lo chiede esplicitamente, altrimenti procedi senza memoria.
2. **Dopo una decisione di design/architettura non banale**: scelta di una libreria, di un pattern,
   di uno schema dati, di un trade-off di performance/sicurezza, di una struttura di moduli.
3. **Prima di modificare un'area di codice** che non hai toccato in questa sessione: controlla se è
   già stata marcata o se ci sono decisioni registrate che la riguardano.
4. **Quando l'utente chiede esplicitamente**: "cosa avevamo deciso su...", "perché abbiamo scelto...",
   "riprendiamo da dove eravamo rimasti", "salva questa decisione", "ricordi qualcosa su questo modulo?".
5. **Fine sessione/task lungo**: se hai chiamato `tokensave_session_start`, chiudi con
   `tokensave_session_end` per mostrare il delta.

## Procedura

### A. Avvio sessione

1. Chiama `tokensave_session_start`. Ti torna un `memory_delta` con fino a 5 decisioni e 5 code-area
   recenti: usalo per un breve riepilogo "dove eravamo rimasti" prima di iniziare a lavorare — non
   serve raccontarlo per intero all'utente, basta che orienti le tue scelte.
2. Se il task riguarda un'area specifica (es. "lavoriamo sull'upload chunked"), chiama anche
   `tokensave_session_recall` con una query pertinente (es. `"upload" OR "chunked"`) per recuperare
   decisioni passate che altrimenti l'utente dovrebbe rispiegarti.

### B. Durante il lavoro

- **Prima di una modifica strutturale** a un file/modulo: chiama `tokensave_session_recall` con la
  query sul nome del modulo/simbolo. Se trovi decisioni pregresse in conflitto con quello che stai per
  fare, segnalalo all'utente prima di procedere — non sovrascrivere silenziosamente una scelta passata.
- **Quando prendi/concordi con l'utente una decisione di design**, chiama subito
  `tokensave_record_decision` con:
  - `decision`: frase breve e dichiarativa (es. "Usare SHA-256 streaming via @noble/hashes invece di
    leggere l'intero file in memoria per l'upload chunked")
  - `reason`: il perché, in 1-2 frasi (trade-off, vincolo, alternativa scartata)
  - `files`: i path coinvolti, se noti
  - `tags`: 1-4 tag brevi e riusabili (es. `["upload", "performance", "angular"]`)
- **Quando finisci di lavorare in modo non banale su un path**, chiama `tokensave_record_code_area`
  per quel path. Non serve farlo per ogni piccolo edit: usalo per moduli/feature, non per singole righe.

### C. Fine sessione

- Se hai chiamato `tokensave_session_start`, chiudi con `tokensave_session_end` e riporta all'utente
  in 1-2 righe cosa è migliorato/peggiorato (se rilevante), senza dump di metriche grezze a meno che
  non le chieda.

## Linee guida su cosa registrare

**Registra**: scelte architetturali, trade-off espliciti, pattern adottati per un motivo preciso,
convenzioni di progetto, vincoli tecnici scoperti facendo debugging, decisioni di sicurezza/permessi.

**Non registrare**: dettagli implementativi banali, refactoring puramente stilistici, TODO generici
(quelli vanno nel backlog/issue tracker, non nella memoria architetturale), segreti o credenziali,
informazioni personali dell'utente non legate al codice.

**Una decisione per chiamata.** Non accorpare più decisioni scorrelate in un solo `record_decision`:
rende `session_recall` meno utile in futuro perché il match FTS5 perde precisione.

## Esempio end-to-end

```
Utente: "Riprendiamo il lavoro sul progetto, oggi voglio rivedere la gestione dei permessi"

1. tokensave_session_start
   → memory_delta: ["Decisione: la logica di ereditarietà dei permessi va gestita solo nel livello Service, non direttamente nei controller", "Code area: src/Permissions/* (toccata 6 volte, ultima 3 giorni fa)"]

2. tokensave_session_recall(query: "permission inheritance")
   → trova la decisione sopra + eventuali altre sulle aree correlate

3. [lavoro sul codice...]

4. Decisione presa con l'utente: "Aggiungere un controllo di ownership prima di rompere l'ereditarietà"
   → tokensave_record_decision(
       decision: "Aggiunto controllo ownership prima di modificare l'ereditarietà permessi",
       reason: "Evitare che un utente con permesso di scrittura ma non owner modifichi la struttura ACL",
       files: ["src/Permissions/PermissionService.cs"],
       tags: ["permissions", "security", "ownership"]
     )

5. tokensave_record_code_area(path: "src/Permissions/")

6. Fine sessione → tokensave_session_end
```

## Se i tool tokensave non sono disponibili

Se `tokensave_*` non compare tra i tool MCP disponibili, il server non è registrato per questo
agente/progetto. Non improvvisare una memoria alternativa nel codice o in file ad-hoc: dillo
all'utente e suggerisci `tokensave install --agent antigravity` (globale) o
`tokensave install --local --agent antigravity` (solo per questo progetto), seguito da `tokensave init`
se il progetto non è ancora indicizzato.
