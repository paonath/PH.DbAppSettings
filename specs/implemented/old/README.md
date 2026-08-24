# YAML Front Matter Convention

Ogni file markdown di tipo spec o execution-plan DEVE includere uno YAML front matter strutturato all'inizio del file.
Questo garantisce metadati leggibili a macchina (tipo, versione, stato, autore, data), coerenza e tracciabilità tra i documenti.

## Schema `spec`
Da usare per documenti di specifica tecnica.
Il campo `status` ammette i valori: `draft`, `review`, `approved`, `deprecated`.

**Esempio:**
```yaml
---
type: spec
title: "Architettura del Sistema"
version: "1.0.0"
status: draft
created: 2026-05-14
updated: 2026-05-14
author: "Team Architecture"
project: "PH.DbAppSettings"
tags: [architecture, design]
---
```

## Schema `execution-plan`
Da usare per piani di implementazione atomici destinati a un'AI.
Il campo `status` ammette i valori: `draft`, `ready`, `in-progress`, `completed`.

**Esempio:**
```yaml
---
type: execution-plan
title: "Implementazione API Tipizzata"
version: "1.0.0"
status: ready
created: 2026-05-14
updated: 2026-05-14
author: "AI Developer"
project: "PH.DbAppSettings"
target: ai
task-count: 5
tags: [api, typed]
---
```
